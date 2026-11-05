using System;
using System.Collections.Generic;
using System.Text;
using ToDoList.Data;
using ToDoList.Models;
using SQLite;

namespace ToDoList.Core.Repositories
{
    public class TaskRepository
    {
        private readonly AppDatabase _database;

        public TaskRepository(AppDatabase database)
        {
            _database = database;
        }

        public async Task<int> AddAsync(TaskItem task)
        {
            SQLiteAsyncConnection connection = await _database.GetConnectionAsync();

            return await connection.InsertAsync(task);
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            SQLiteAsyncConnection connection = await _database.GetConnectionAsync();

            return await connection.Table<TaskItem>().ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            SQLiteAsyncConnection connection = await _database.GetConnectionAsync();

            return await connection.Table<TaskItem>().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<int> UpdateAsync(TaskItem task)
        {
            SQLiteAsyncConnection connection = await _database.GetConnectionAsync();

            return await connection.UpdateAsync(task);
        }

        public async Task<int> DeleteAsync(int id)
        {
            SQLiteAsyncConnection connection = await _database.GetConnectionAsync();

            return await connection.DeleteAsync<TaskItem>(id);
        }

    }
}
