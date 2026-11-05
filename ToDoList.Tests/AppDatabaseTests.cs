using SQLite;
using ToDoList.Data;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;
using ToDoList.Models;

namespace ToDoList.Tests
{
    public class AppDatabaseTests
    {
        private class TableInfo
        {
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task InitializeAsync_ShouldDBExist()
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
                );

            AppDatabase database = new AppDatabase(databasePath);

            try
            {
                await database.InitializeAsync();

                Assert.True(File.Exists(databasePath));
            }
            finally
            {
                await database.CloseAsync();

                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }

        [Fact]
        public async Task InitializeAsync_ShouldCreateTables()
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            AppDatabase database = new AppDatabase(databasePath);

            try
            {
                await database.InitializeAsync();

                SQLiteAsyncConnection connection =
                    new SQLiteAsyncConnection(databasePath);

                try
                {
                    List<TableInfo> tables = await connection.QueryAsync<TableInfo>(
                        "SELECT name AS Name FROM sqlite_master WHERE type = 'table';"
                    );

                    Assert.Contains(tables, t => t.Name == "Category");
                    Assert.Contains(tables, t => t.Name == "TaskItem");
                    Assert.Contains(tables, t => t.Name == "Subtask");
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            finally
            {
                await database.CloseAsync();

                if (File.Exists(databasePath))
                    File.Delete(databasePath);
            }
        }

        [Fact]
        public async Task InitializeAsync_ShouldBeFiveCategories()
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            AppDatabase database = new AppDatabase(databasePath);

            try
            {
                await database.InitializeAsync();

                SQLiteAsyncConnection connection =
                    new SQLiteAsyncConnection(databasePath);

                try
                {
                    int count = await connection.Table<Category>().CountAsync();

                    Assert.Equal(5, count);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            finally
            {
                await database.CloseAsync();

                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }

        [Fact]
        public async Task InitializeAsync_ShouldNotDuplicateCategories()
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db3"
            );

            AppDatabase database = new AppDatabase(databasePath);

            try
            {
                await database.InitializeAsync();

                await database.CloseAsync();

                await database.InitializeAsync();

                SQLiteAsyncConnection connection = new SQLiteAsyncConnection(databasePath);

                try
                {
                    int count = await connection.Table<Category>().CountAsync();

                    Assert.Equal(5, count);
                }
                finally
                {
                    await connection.CloseAsync();
                }

            }

            finally
            {
                await database.CloseAsync();

                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }
    }
}
