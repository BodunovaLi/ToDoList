
using SQLite;
using ToDoList.Core.Repositories;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Tests
{
    public class TaskRepositoryTests : IAsyncLifetime
    {
        private readonly string _databasePath;
        private readonly AppDatabase _database;
        private readonly TaskRepository _repos;

        public TaskRepositoryTests()
        {
            _databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            _database = new AppDatabase(_databasePath);
            _repos = new TaskRepository(_database);
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
        public async Task AddAsync_WithExistingCategory_ShouldSaveTask()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _repos.AddAsync(item);

            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            TaskItem? savedItem = await connection
                .Table<TaskItem>()
                .FirstOrDefaultAsync(t => t.Id == item.Id);

            Assert.NotNull(savedItem);
            Assert.True(item.Id > 0);
            Assert.Equal(item.Name, savedItem.Name);
            Assert.Equal(item.CategoryId, savedItem.CategoryId);
            Assert.Equal(item.IsCompleted, savedItem.IsCompleted);
            Assert.Equal(item.CreatedAt, savedItem.CreatedAt);
            Assert.Equal(item.DueDate, savedItem.DueDate);
        }

        [Fact]
        public async Task AddAsync_WithNonExistingCategory_ShouldThrowException()
        {
            TaskItem item = new TaskItem
            {
                Name = "Тестовая задача",
                CategoryId = 999,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            SQLiteException exception =
                await Assert.ThrowsAsync<SQLiteException>(
                    async () => await _repos.AddAsync(item)
                );

            Assert.Equal(SQLite3.Result.Constraint, exception.Result);
        }

        [Fact]
        public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
        {
            List<TaskItem> tasks = await _repos.GetAllAsync();

            Assert.Empty(tasks);
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleTasks_ShouldReturnAllTasks()
        {
            TaskItem item = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            TaskItem item1 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 11, 4),
                DueDate = new DateTime(2026, 11, 10)
            };

            await _repos.AddAsync(item);
            await _repos.AddAsync(item1);

            List<TaskItem> tasks = await _repos.GetAllAsync();

            Assert.Equal(2, tasks.Count);
            Assert.Contains(tasks,
                t => t.Id == item.Id && t.Name == item.Name);
            Assert.Contains(tasks,
                t => t.Id == item1.Id && t.Name == item1.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnTask()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _repos.AddAsync(item1);
            await _repos.AddAsync(item2);

            TaskItem? resultItem =
                await _repos.GetByIdAsync(item2.Id);

            Assert.NotNull(resultItem);
            Assert.Equal(item2.Id, resultItem.Id);
            Assert.Equal(item2.Name, resultItem.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _repos.AddAsync(item1);
            await _repos.AddAsync(item2);

            int nonExistingId = Math.Max(item1.Id, item2.Id) + 1;

            TaskItem? resultItem =
                await _repos.GetByIdAsync(nonExistingId);

            Assert.Null(resultItem);
        }


        [Fact]
        public async Task UpdateAsync_WithExistingTask_ShouldUpdateTask()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _repos.AddAsync(item1);
            await _repos.AddAsync(item2);

            TaskItem? item = await _repos.GetByIdAsync(item1.Id);

            Assert.NotNull(item);

            item.Name = "Купить молоко";
            item.CategoryId = 2;
            item.CreatedAt = new DateTime(2026, 12, 5);
            item.IsCompleted = true;

            int updatedRows = await _repos.UpdateAsync(item);

            TaskItem? newItem = await _repos.GetByIdAsync(item1.Id);

            Assert.NotNull(newItem);
            Assert.Equal(1, updatedRows);
            Assert.Equal(item.Id, newItem.Id);
            Assert.Equal(item.Name, newItem.Name);
            Assert.Equal(item.CategoryId, newItem.CategoryId);
            Assert.Equal(item.CreatedAt, newItem.CreatedAt);
            Assert.Equal(item.IsCompleted, newItem.IsCompleted);

            TaskItem? unchangedItem = await _repos.GetByIdAsync(item2.Id);

            Assert.NotNull(unchangedItem);
            Assert.Equal("Полить цветы", unchangedItem.Name);
            Assert.Equal(5, unchangedItem.CategoryId);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingTask_ShouldReturnZero()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };


            await _repos.AddAsync(item1);

            TaskItem newItem = new TaskItem
            {
                Id = item1.Id + 1,
                Name = "Купить молоко",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 10, 6)
            };

            int updatedRows = await _repos.UpdateAsync(newItem);

            Assert.Equal(0, updatedRows);

            TaskItem? savedItem = await _repos.GetByIdAsync(item1.Id);

            Assert.NotNull(savedItem);
            Assert.Equal("Купить продукты", savedItem.Name);

        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingCategory_ShouldThrowException()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };


            await _repos.AddAsync(item1);

            TaskItem? itemUpdated = await _repos.GetByIdAsync(item1.Id);

            Assert.NotNull(itemUpdated);

            itemUpdated.CategoryId = 999;

            SQLiteException exception =await Assert.ThrowsAsync<SQLiteException>(
                async () => await _repos.UpdateAsync(itemUpdated)
            );

            Assert.Equal(SQLite3.Result.Constraint, exception.Result);

            TaskItem? savedItem = await _repos.GetByIdAsync(item1.Id);

            Assert.NotNull(savedItem);
            Assert.Equal(4, savedItem.CategoryId);

        }

        [Fact]
        public async Task DeleteAsync_WithExistingTask_ShouldDeleteTask()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _repos.AddAsync(item1);
            await _repos.AddAsync(item2);

            int deletedRows = await _repos.DeleteAsync(item2.Id);

            Assert.Equal(1, deletedRows);

            Assert.Null(await _repos.GetByIdAsync(item2.Id));
            Assert.NotNull(await _repos.GetByIdAsync(item1.Id));
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingTask_ShouldReturnZero()
        {
            TaskItem item1 = new TaskItem
            {
                Name = "Купить продукты",
                CategoryId = 4,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            TaskItem item2 = new TaskItem
            {
                Name = "Полить цветы",
                CategoryId = 5,
                CreatedAt = new DateTime(2026, 11, 4)
            };

            await _repos.AddAsync(item1);
            await _repos.AddAsync(item2);

            int deletedId = Math.Max(item1.Id, item2.Id) + 1;

            int deletedRows = await _repos.DeleteAsync(deletedId);

            Assert.Equal(0, deletedRows);

            Assert.NotNull(await _repos.GetByIdAsync(item2.Id));
            Assert.NotNull(await _repos.GetByIdAsync(item1.Id));
        }


    }
}
