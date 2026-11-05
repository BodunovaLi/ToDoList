
using SQLite;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Core.Repositories
{
    public class SubtaskRepository
    {
        private readonly AppDatabase _database;

        public SubtaskRepository(AppDatabase database)
        {
            _database = database;
        }

        public async Task<int> AddAsync(Subtask subtask)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.InsertAsync(subtask);
        }

        public async Task<List<Subtask>> GetByTaskIdAsync(int taskId)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.Table<Subtask>()
                .Where(s => s.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<Subtask?> GetByIdAsync(int id)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.Table<Subtask>()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<int> UpdateAsync(Subtask subtask)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.UpdateAsync(subtask);
        }

        public async Task<int> DeleteAsync(int id)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            return await connection.DeleteAsync<Subtask>(id);
        }
    }
}
