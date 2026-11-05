
using SQLite;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Core.Repositories
{
    public class CategoryRepository
    {
        private readonly AppDatabase _database;

        public CategoryRepository(AppDatabase database)
        {
            _database = database;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.Table<Category>()
                .OrderBy(c => c.Id)
                .ToListAsync();
        }
    }
}
