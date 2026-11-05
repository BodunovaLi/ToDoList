
using SQLite;
using ToDoList.Core.Repositories;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Tests
{
    public class SubtaskRepositoryTests : IAsyncLifetime
    {
        private readonly string _databasePath;
        private readonly AppDatabase _database;
        private readonly TaskRepository _reposForTasks;
        private readonly SubtaskRepository _reposForSubtasks;

        public SubtaskRepositoryTests()
        {
            _databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            _database = new AppDatabase(_databasePath);
            _reposForTasks = new TaskRepository(_database);
            _reposForSubtasks = new SubtaskRepository(_database);
        }

        public async Task InitializeAsync()
        {
            await _database.GetConnectionAsync();
        }

        public async Task DisposeAsync()
        {
            await _database.CloseAsync();

            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }

        [Fact]
        public async Task AddAsync_WithExistingTask_ShouldSaveSubtask()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Купить молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);

            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            Subtask? savedItem = await connection
                .Table<Subtask>()
                .FirstOrDefaultAsync(s => s.Id == subtask1.Id);

            Assert.NotNull(savedItem);
            Assert.True(subtask1.Id > 0);
            Assert.Equal(subtask1.Id, savedItem.Id);
            Assert.Equal(subtask1.Name, savedItem.Name);
            Assert.Equal(item.Id, savedItem.TaskId);
            Assert.Equal(subtask1.IsCompleted, savedItem.IsCompleted);
        }

        [Fact]
        public async Task AddAsync_WithNonExistingTask_ShouldThrowException()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Купить молоко",
                TaskId = item.Id + 1,
                IsCompleted = false
            };

            SQLiteException exception =
                await Assert.ThrowsAsync<SQLiteException>(
                    async () =>
                        await _reposForSubtasks.AddAsync(subtask1)
                );

            Assert.Equal(SQLite3.Result.Constraint, exception.Result);
        }

        [Fact]
        public async Task GetByTaskIdAsync_WithExistingTask_ShouldReturnSubtasks()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Сделать уборку",
                CategoryId = 3,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 12, 5),
                DueDate = new DateTime(2026, 12, 7)
            };

            await _reposForTasks.AddAsync(item1);
            await _reposForTasks.AddAsync(item2);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item1.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Йогурт",
                TaskId = item1.Id,
                IsCompleted = false
            };

            Subtask subtask3 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item1.Id,
                IsCompleted = false
            };

            Subtask subtask4 = new Subtask
            {
                Name = "Протереть пыль",
                TaskId = item2.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);
            await _reposForSubtasks.AddAsync(subtask3);
            await _reposForSubtasks.AddAsync(subtask4);

            List<Subtask> resultForItem1 =
                await _reposForSubtasks.GetByTaskIdAsync(item1.Id);

            List<Subtask> resultForItem2 =
                await _reposForSubtasks.GetByTaskIdAsync(item2.Id);

            Assert.Equal(3, resultForItem1.Count);
            Assert.Single(resultForItem2);

            Assert.All(resultForItem1,
                subtask => Assert.Equal(item1.Id, subtask.TaskId));

            Assert.All(resultForItem2,
                subtask => Assert.Equal(item2.Id, subtask.TaskId));

            Assert.Contains(resultForItem1,
                subtask => subtask.Id == subtask1.Id);

            Assert.Contains(resultForItem1,
                subtask => subtask.Id == subtask2.Id);

            Assert.Contains(resultForItem1,
                subtask => subtask.Id == subtask3.Id);

            Assert.Equal(subtask4.Id, resultForItem2[0].Id);
        }


        [Fact]
        public async Task GetByTaskIdAsync_WithNoSubtasks_ShouldReturnEmptyList()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _reposForTasks.AddAsync(item);

            List<Subtask> result =
                await _reposForSubtasks.GetByTaskIdAsync(item.Id);

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnSubtask()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item.Id,
                IsCompleted = true
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);

            Subtask? result =
                await _reposForSubtasks.GetByIdAsync(subtask2.Id);

            Assert.NotNull(result);
            Assert.Equal(subtask2.Id, result.Id);
            Assert.Equal(subtask2.Name, result.Name);
            Assert.Equal(subtask2.TaskId, result.TaskId);
            Assert.Equal(subtask2.IsCompleted, result.IsCompleted);
        }


        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);

            int nonExistingId =
                Math.Max(subtask1.Id, subtask2.Id) + 1;

            Subtask? result =
                await _reposForSubtasks.GetByIdAsync(nonExistingId);

            Assert.Null(result);
        }


        [Fact]
        public async Task UpdateAsync_WithExistingSubtask_ShouldUpdateSubtask()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);

            Subtask? subtaskUpdated =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Assert.NotNull(subtaskUpdated);

            subtaskUpdated.Name = "Йогурт";
            subtaskUpdated.IsCompleted = true;

            int updatedRows =
                await _reposForSubtasks.UpdateAsync(subtaskUpdated);

            Subtask? result =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Subtask? unchangedSubtask =
                await _reposForSubtasks.GetByIdAsync(subtask2.Id);

            Assert.Equal(1, updatedRows);

            Assert.NotNull(result);
            Assert.Equal(subtask1.Id, result.Id);
            Assert.Equal("Йогурт", result.Name);
            Assert.Equal(item.Id, result.TaskId);
            Assert.True(result.IsCompleted);

            Assert.NotNull(unchangedSubtask);
            Assert.Equal(subtask2.Id, unchangedSubtask.Id);
            Assert.Equal("Хлеб", unchangedSubtask.Name);
            Assert.False(unchangedSubtask.IsCompleted);
        }


        [Fact]
        public async Task UpdateAsync_WithNonExistingSubtask_ShouldReturnZero()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);

            Subtask nonExistingSubtask = new Subtask
            {
                Id = subtask1.Id + 1,
                Name = "Йогурт",
                TaskId = item.Id,
                IsCompleted = true
            };

            int updatedRows =
                await _reposForSubtasks.UpdateAsync(nonExistingSubtask);

            Subtask? result =
                await _reposForSubtasks.GetByIdAsync(nonExistingSubtask.Id);

            Subtask? existingSubtask =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Assert.Equal(0, updatedRows);
            Assert.Null(result);

            Assert.NotNull(existingSubtask);
            Assert.Equal("Молоко", existingSubtask.Name);
            Assert.False(existingSubtask.IsCompleted);
        }


        [Fact]
        public async Task UpdateAsync_WithNonExistingTask_ShouldThrowException()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);

            Subtask? subtaskUpdated =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Assert.NotNull(subtaskUpdated);

            subtaskUpdated.TaskId = item.Id + 1;

            SQLiteException exception =
                await Assert.ThrowsAsync<SQLiteException>(
                    async () =>
                        await _reposForSubtasks.UpdateAsync(subtaskUpdated)
                );

            Assert.Equal(SQLite3.Result.Constraint, exception.Result);

            Subtask? result =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Assert.NotNull(result);
            Assert.Equal(item.Id, result.TaskId);
            Assert.Equal("Молоко", result.Name);
        }


        [Fact]
        public async Task DeleteAsync_WithExistingSubtask_ShouldDeleteSubtask()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);

            int deletedRows =
                await _reposForSubtasks.DeleteAsync(subtask2.Id);

            Subtask? deletedSubtask =
                await _reposForSubtasks.GetByIdAsync(subtask2.Id);

            Subtask? remainingSubtask =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Assert.Equal(1, deletedRows);
            Assert.Null(deletedSubtask);

            Assert.NotNull(remainingSubtask);
            Assert.Equal(subtask1.Id, remainingSubtask.Id);
            Assert.Equal("Молоко", remainingSubtask.Name);
        }


        [Fact]
        public async Task DeleteAsync_WithNonExistingSubtask_ShouldReturnZero()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _reposForTasks.AddAsync(item);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);

            int nonExistingId =
                Math.Max(subtask1.Id, subtask2.Id) + 1;

            int deletedRows =
                await _reposForSubtasks.DeleteAsync(nonExistingId);

            Subtask? result1 =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Subtask? result2 =
                await _reposForSubtasks.GetByIdAsync(subtask2.Id);

            Assert.Equal(0, deletedRows);

            Assert.NotNull(result1);
            Assert.NotNull(result2);

            Assert.Equal("Молоко", result1.Name);
            Assert.Equal("Хлеб", result2.Name);
        }


        [Fact]
        public async Task DeleteTask_ShouldDeleteRelatedSubtasks()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Сделать уборку",
                CategoryId = 3,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 5)
            };

            await _reposForTasks.AddAsync(item1);
            await _reposForTasks.AddAsync(item2);

            Subtask subtask1 = new Subtask
            {
                Name = "Молоко",
                TaskId = item1.Id,
                IsCompleted = false
            };

            Subtask subtask2 = new Subtask
            {
                Name = "Хлеб",
                TaskId = item1.Id,
                IsCompleted = false
            };

            Subtask subtask3 = new Subtask
            {
                Name = "Протереть пыль",
                TaskId = item2.Id,
                IsCompleted = false
            };

            await _reposForSubtasks.AddAsync(subtask1);
            await _reposForSubtasks.AddAsync(subtask2);
            await _reposForSubtasks.AddAsync(subtask3);


            List<Subtask> subtasksBeforeDelete =
                await _reposForSubtasks.GetByTaskIdAsync(item1.Id);

            Assert.Equal(2, subtasksBeforeDelete.Count);

        

            int deletedRows =
                await _reposForTasks.DeleteAsync(item1.Id);

            TaskItem? deletedTask =
                await _reposForTasks.GetByIdAsync(item1.Id);

            TaskItem? remainingTask =
                await _reposForTasks.GetByIdAsync(item2.Id);

            List<Subtask> deletedSubtasks =
                await _reposForSubtasks.GetByTaskIdAsync(item1.Id);

            List<Subtask> remainingSubtasks =
                await _reposForSubtasks.GetByTaskIdAsync(item2.Id);

            Subtask? deletedSubtask1 =
                await _reposForSubtasks.GetByIdAsync(subtask1.Id);

            Subtask? deletedSubtask2 =
                await _reposForSubtasks.GetByIdAsync(subtask2.Id);

            Assert.Equal(1, deletedRows);

            Assert.Null(deletedTask);
            Assert.NotNull(remainingTask);

            Assert.Empty(deletedSubtasks);
            Assert.Null(deletedSubtask1);
            Assert.Null(deletedSubtask2);

            Assert.Single(remainingSubtasks);
            Assert.Equal(subtask3.Id, remainingSubtasks[0].Id);
            Assert.Equal(item2.Id, remainingSubtasks[0].TaskId);
        }

    }
}
