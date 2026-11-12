
using ToDoList.Core.Repositories;
using ToDoList.Core.DTOs;
using ToDoList.Data;
using ToDoList.Models;
using ToDoList.Services;

namespace ToDoList.Tests.Services;

public class TaskServiceTests : IAsyncLifetime
{
    [Fact]
    public async Task SetTaskCompletedAsync_ShouldPersistBothStatesAndPreserveTaskDetails()
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Checkbox task",
            CategoryId = 2,
            DueDate = DateTime.Today,
            Subtasks = new() { new() { Name = "Pending subtask" } }
        });

        foreach (var isCompleted in new[] { true, false })
        {
            await _taskService.SetTaskCompletedAsync(task.Id, isCompleted);

            var saved = await _taskService.GetTaskByIdAsync(task.Id);
            Assert.NotNull(saved);
            Assert.Equal(isCompleted, saved.IsCompleted);
            Assert.Equal(task.Name, saved.Name);
            Assert.Equal(task.CategoryId, saved.CategoryId);
            Assert.Equal(task.DueDate, saved.DueDate);
            Assert.Equal(task.CreatedAt, saved.CreatedAt);
            var subtask = Assert.Single(await _subtaskRepository.GetByTaskIdAsync(task.Id));
            Assert.False(subtask.IsCompleted);
        }
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldSynchronizeSubtasksAndKeepTaskIdentity()
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Original", CategoryId = 2,
            Subtasks = new() { new() { Name = "Keep" }, new() { Name = "Remove" } }
        });
        var previous = await _subtaskRepository.GetByTaskIdAsync(task.Id);
        var keptId = previous.Single(s => s.Name == "Keep").Id;
        var removedId = previous.Single(s => s.Name == "Remove").Id;

        await _taskService.UpdateTaskAsync(new UpdateTaskRequest
        {
            Id = task.Id, Name = "Updated", CategoryId = 3, DueDate = DateTime.Today.AddDays(2),
            Subtasks = new()
            {
                new() { Id = keptId, Name = " Renamed ", IsCompleted = true },
                new() { Name = "New" }
            }
        });

        var saved = await _taskService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(saved);
        Assert.Equal("Updated", saved.Name);
        Assert.Equal(3, saved.CategoryId);
        Assert.Equal(task.CreatedAt, saved.CreatedAt);
        Assert.Equal(DateTime.Today.AddDays(2), saved.DueDate);
        Assert.Single(await _taskService.GetAllTasksAsync());
        var subtasks = await _subtaskRepository.GetByTaskIdAsync(task.Id);
        Assert.Equal(2, subtasks.Count);
        Assert.True(subtasks.Single(s => s.Id == keptId).IsCompleted);
        Assert.Equal("Renamed", subtasks.Single(s => s.Id == keptId).Name);
        Assert.DoesNotContain(subtasks, s => s.Id == removedId);
        Assert.True(subtasks.Single(s => s.Name == "New").Id > 0);
        Assert.All(subtasks, s => Assert.Equal(task.Id, s.TaskId));
    }

    [Fact]
    public async Task UpdateTaskAsync_EmptySubtasks_ShouldRemoveAllAndClearDate()
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Original", DueDate = DateTime.Today,
            Subtasks = new() { new() { Name = "Remove" } }
        });
        await _taskService.UpdateTaskAsync(new UpdateTaskRequest
        {
            Id = task.Id, Name = task.Name, CategoryId = task.CategoryId, Subtasks = new()
        });
        Assert.Empty(await _subtaskRepository.GetByTaskIdAsync(task.Id));
        Assert.Null((await _taskService.GetTaskByIdAsync(task.Id))!.DueDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateTaskAsync_InvalidSubtaskIdentity_ShouldLeaveDatabaseUnchanged(bool duplicate)
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Original", Subtasks = new() { new() { Name = "Keep" } }
        });
        var other = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Other", Subtasks = new() { new() { Name = "Foreign" } }
        });
        var own = (await _subtaskRepository.GetByTaskIdAsync(task.Id)).Single();
        var foreign = (await _subtaskRepository.GetByTaskIdAsync(other.Id)).Single();
        var drafts = new List<UpdateSubtaskDraft>
        {
            new() { Id = duplicate ? own.Id : foreign.Id, Name = "Bad update" }
        };
        if (duplicate) drafts.Add(new() { Id = own.Id, Name = "Duplicate" });

        await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(new UpdateTaskRequest
        {
            Id = task.Id, Name = "Must not save", CategoryId = 3, Subtasks = drafts
        }));
        Assert.Equal("Original", (await _taskService.GetTaskByIdAsync(task.Id))!.Name);
        Assert.Equal("Keep", (await _subtaskRepository.GetByTaskIdAsync(task.Id)).Single().Name);
        Assert.Equal("Foreign", (await _subtaskRepository.GetByTaskIdAsync(other.Id)).Single().Name);
    }

    [Fact]
    public async Task UpdateTaskAsync_BlankSubtask_ShouldNotSaveTaskChanges()
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest { Name = "Original" });
        await Assert.ThrowsAsync<ArgumentException>(() => _taskService.UpdateTaskAsync(new UpdateTaskRequest
        {
            Id = task.Id, Name = "Must not save", CategoryId = 3,
            Subtasks = new() { new() { Name = " " } }
        }));
        Assert.Equal("Original", (await _taskService.GetTaskByIdAsync(task.Id))!.Name);
    }

    private AppDatabase _database = null!;
    private TaskService _taskService = null!;
    private SubtaskRepository _subtaskRepository = null!;
    private string _databasePath = string.Empty;

    public async Task InitializeAsync()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.db3");

        _database = new AppDatabase(_databasePath);
        await _database.GetConnectionAsync();

        var taskRepository = new TaskRepository(_database);
        _subtaskRepository = new SubtaskRepository(_database);
        var categoryRepository = new CategoryRepository(_database);

        _taskService = new TaskService(
            taskRepository,
            categoryRepository);
    }

    public async Task DisposeAsync()
    {
        await _database.CloseAsync();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }


    [Fact]
    public async Task CreateTaskAsync_WithoutSubtasks_ShouldCreateTask()
    {
        var request = new CreateTaskRequest
        {
            Name = "Подготовиться к экзамену",
            CategoryId = 2,
            DueDate = DateTime.Today.AddDays(3)
        };
       
        TaskItem createdTask =
            await _taskService.CreateTaskAsync(request);
        TaskItem? savedTask = await _taskService.GetTaskByIdAsync(createdTask.Id);


        Assert.NotNull(savedTask);
        Assert.Equal(createdTask.Name, savedTask.Name);
        Assert.True(createdTask.Id > 0);
        Assert.Equal("Подготовиться к экзамену", createdTask.Name);
        Assert.Equal(2, createdTask.CategoryId);
        Assert.False(createdTask.IsCompleted);
        Assert.Equal(request.DueDate, createdTask.DueDate);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldPreserveSubtaskCompletion()
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Name = "Task with completed draft",
            Subtasks = new List<SubtaskDraft>
            {
                new() { Name = "Done", IsCompleted = true },
                new() { Name = "Pending", IsCompleted = false }
            }
        });

        var saved = await _subtaskRepository.GetByTaskIdAsync(task.Id);

        Assert.Equal(2, saved.Count);
        Assert.True(saved.Single(s => s.Name == "Done").IsCompleted);
        Assert.False(saved.Single(s => s.Name == "Pending").IsCompleted);
        Assert.All(saved, subtask => Assert.Equal(task.Id, subtask.TaskId));
    }


    [Fact]
    public async Task CreateTaskAsync_WithSubtasks_ShouldSaveAllSubtasks()
    {
        var request = new CreateTaskRequest
        {
            Name = "Купить продукты",
            CategoryId = 4,
            Subtasks = new List<SubtaskDraft>
        {
            new() { Name = "Молоко" },
            new() { Name = "Хлеб" },
            new() { Name = "Яйца" }
        }
        };

        TaskItem createdTask =
            await _taskService.CreateTaskAsync(request);

        List<Subtask> savedSubtasks =
            await _subtaskRepository.GetByTaskIdAsync(createdTask.Id);

        Assert.Equal(3, savedSubtasks.Count);
        Assert.All(savedSubtasks,
            subtask => Assert.Equal(createdTask.Id, subtask.TaskId));

        Assert.Contains(savedSubtasks, s => s.Name == "Молоко");
        Assert.Contains(savedSubtasks, s => s.Name == "Хлеб");
        Assert.Contains(savedSubtasks, s => s.Name == "Яйца");
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTaskAsync_WithEmptyName_ShouldThrowException(
        string name)
    {
        var request = new CreateTaskRequest
        {
            Name = name
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.CreateTaskAsync(request));
    }


    [Fact]
    public async Task CreateTaskAsync_WithPastDueDate_ShouldThrowException()
    {
        var request = new CreateTaskRequest
        {
            Name = "Просроченная задача",
            DueDate = DateTime.Today.AddDays(-1)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.CreateTaskAsync(request));
    }


    [Fact]
    public async Task CreateTaskAsync_WithoutCategory_ShouldUseDefaultCategory()
    {
        var request = new CreateTaskRequest
        {
            Name = "Личная задача"
        };

        TaskItem task = await _taskService.CreateTaskAsync(request);

        Assert.Equal(5, task.CategoryId);
    }


    [Fact]
    public async Task CreateTaskAsync_WithInvalidCategory_ShouldThrowException()
    {
        var request = new CreateTaskRequest
        {
            Name = "Новая задача",
            CategoryId = 999
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.CreateTaskAsync(request));
    }


    [Fact]
    public async Task CreateTaskAsync_WithEmptySubtaskName_ShouldThrowException()
    {
        var request = new CreateTaskRequest
        {
            Name = "Купить продукты",
            Subtasks = new List<SubtaskDraft>
        {
            new() { Name = "Молоко" },
            new() { Name = "   " }
        }
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.CreateTaskAsync(request));

        List<TaskItem> tasks = await _taskService.GetAllTasksAsync();

        Assert.Empty(tasks);
    }


    [Fact]
    public async Task UpdateTaskAsync_ValidName_ShouldUpdateName()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Старое название" });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Новое название",
            CategoryId = task.CategoryId,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Equal("Новое название", saved.Name);
    }


    [Fact]
    public async Task UpdateTaskAsync_ValidCategory_ShouldUpdateCategory()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача", CategoryId = 5 });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = task.Name,
            CategoryId = 2,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Equal(2, saved.CategoryId);
    }


    [Fact]
    public async Task UpdateTaskAsync_ValidDueDate_ShouldUpdateDueDate()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var newDate = DateTime.Today.AddDays(7);

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = task.Name,
            CategoryId = task.CategoryId,
            DueDate = newDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Equal(newDate, saved.DueDate);
    }

  
    [Fact]
    public async Task UpdateTaskAsync_NullDueDate_ShouldRemoveDueDate()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest
            {
                Name = "Задача",
                DueDate = DateTime.Today.AddDays(5)
            });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = task.Name,
            CategoryId = task.CategoryId,
            DueDate = null
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Null(saved.DueDate);
    }


    [Fact]
    public async Task UpdateTaskAsync_ShouldPreserveIdAndCreatedAt()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        int originalId = task.Id;
        DateTime originalCreatedAt = task.CreatedAt;

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Изменённая задача",
            CategoryId = task.CategoryId,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(originalId);

        Assert.NotNull(saved);
        Assert.Equal(originalId, saved.Id);
        Assert.Equal(originalCreatedAt, saved.CreatedAt);
    }

  
    [Fact]
    public async Task UpdateTaskAsync_ShouldPreserveCompletionStatus()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var connection = await _database.GetConnectionAsync();

        task.IsCompleted = true;
        await connection.UpdateAsync(task);

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Новое название",
            CategoryId = task.CategoryId,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.True(saved.IsCompleted);
    }


    [Fact]
    public async Task UpdateTaskAsync_ShouldPreserveSubtasks()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest
            {
                Name = "Покупки",
                Subtasks = new List<SubtaskDraft>
                {
                new() { Name = "Молоко" },
                new() { Name = "Хлеб" }
                }
            });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Покупки домой",
            CategoryId = task.CategoryId,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var subtasks = await _subtaskRepository.GetByTaskIdAsync(task.Id);

        Assert.Equal(2, subtasks.Count);
        Assert.Contains(subtasks, s => s.Name == "Молоко");
        Assert.Contains(subtasks, s => s.Name == "Хлеб");
    }


    [Fact]
    public async Task UpdateTaskAsync_NameWithSpaces_ShouldTrimName()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "   Новое название   ",
            CategoryId = task.CategoryId
        };

        var updated = await _taskService.UpdateTaskAsync(request);

        Assert.Equal("Новое название", updated.Name);
    }


    [Fact]
    public async Task UpdateTaskAsync_NullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _taskService.UpdateTaskAsync(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateTaskAsync_InvalidId_ShouldThrow(int id)
    {
        var request = new UpdateTaskRequest
        {
            Id = id,
            Name = "Задача",
            CategoryId = 5
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _taskService.UpdateTaskAsync(request));
    }

 
    [Fact]
    public async Task UpdateTaskAsync_NonExistingId_ShouldThrow()
    {
        var request = new UpdateTaskRequest
        {
            Id = 99999,
            Name = "Задача",
            CategoryId = 5
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _taskService.UpdateTaskAsync(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateTaskAsync_EmptyName_ShouldThrow(string name)
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = name,
            CategoryId = task.CategoryId
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.UpdateTaskAsync(request));
    }


    [Fact]
    public async Task UpdateTaskAsync_InvalidCategory_ShouldThrow()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Изменённая задача",
            CategoryId = 999
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.UpdateTaskAsync(request));

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Equal("Задача", saved.Name);
    }


    [Fact]
    public async Task UpdateTaskAsync_NewPastDueDate_ShouldThrow()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest { Name = "Задача" });

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = task.Name,
            CategoryId = task.CategoryId,
            DueDate = DateTime.Today.AddDays(-1)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _taskService.UpdateTaskAsync(request));
    }


    [Fact]
    public async Task UpdateTaskAsync_UnchangedPastDueDate_ShouldSucceed()
    {
        var task = await _taskService.CreateTaskAsync(
            new CreateTaskRequest
            {
                Name = "Старая задача",
                DueDate = DateTime.Today.AddDays(1)
            });


        var connection = await _database.GetConnectionAsync();

        task.DueDate = DateTime.Today.AddDays(-5);
        await connection.UpdateAsync(task);

        var request = new UpdateTaskRequest
        {
            Id = task.Id,
            Name = "Новое название",
            CategoryId = task.CategoryId,
            DueDate = task.DueDate
        };

        await _taskService.UpdateTaskAsync(request);

        var saved = await _taskService.GetTaskByIdAsync(task.Id);

        Assert.NotNull(saved);
        Assert.Equal("Новое название", saved.Name);
        Assert.Equal(task.DueDate, saved.DueDate);
    }

}
