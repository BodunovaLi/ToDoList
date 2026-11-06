
using ToDoList.Core.Repositories;
using ToDoList.Core.DTOs;
using ToDoList.Data;
using ToDoList.Models;
using ToDoList.Services;

namespace ToDoList.Tests.Services;

public class TaskServiceTests : IAsyncLifetime
{
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

    // 7. Сохранение подзадач
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
