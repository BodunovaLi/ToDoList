using SQLite;
using ToDoList.Models;

namespace ToDoList.Data
{
    public class AppDatabase
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _databasePath;

        public AppDatabase(string databasePath)
        {
            _databasePath = databasePath;
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_databasePath);

            await _database.ExecuteAsync("PRAGMA foreign_keys = ON;");

            await _database.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS Category (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Color TEXT NOT NULL
                );
                """);

            await _database.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS TaskItem (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    IsCompleted INTEGER NOT NULL,
                    CreatedAt INTEGER NOT NULL,
                    DueDate INTEGER,
                    FOREIGN KEY (CategoryId)
                        REFERENCES Category(Id)
                );
                """);

            await _database.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS Subtask (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    TaskId INTEGER NOT NULL,
                    IsCompleted INTEGER NOT NULL,
                    FOREIGN KEY (TaskId)
                        REFERENCES TaskItem(Id)
                        ON DELETE CASCADE
                );
                """);

            await SeedCategoriesAsync();
        }



        private async Task SeedCategoriesAsync()
        {
            var categories = new[]
            {
                new Category { Id = 1, Name = "Работа", Color = "#EF4444" },
                new Category { Id = 2, Name = "Учеба", Color = "#A78BFA" },
                new Category { Id = 3, Name = "Дом", Color = "#86EFAC" },
                new Category { Id = 4, Name = "Покупки", Color = "#FACC15" },
                new Category { Id = 5, Name = "Личное", Color = "#60A5FA" }
            };

            foreach (var category in categories)
            {
                var existingCategory = await _database!.Table<Category>().FirstOrDefaultAsync(c => c.Id == category.Id);

                if (existingCategory == null)
                {
                    await _database.InsertAsync(category);
                }
            }
        }

        public async Task CloseAsync()
        {
            if (_database == null)
                return;

            await _database.CloseAsync();
            _database = null;
        }
    }
}