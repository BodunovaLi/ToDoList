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

        public async Task AddWithSubtasksAsync(TaskItem task, List<Subtask> subtasks)
        {
            SQLiteAsyncConnection connection =
                await _database.GetConnectionAsync();

            await connection.RunInTransactionAsync(transaction =>
            {
                transaction.Insert(task);

                foreach (Subtask subtask in subtasks)
                {
                    subtask.TaskId = task.Id;
                    transaction.Insert(subtask);
                }
            });
        }

        public async Task UpdateWithSubtasksAsync(TaskItem task, List<Subtask> subtasks)
        {
            var connection = await _database.GetConnectionAsync();
            await connection.RunInTransactionAsync(transaction =>
            {
                var existing = transaction.Table<Subtask>()
                    .Where(subtask => subtask.TaskId == task.Id).ToList();
                var existingIds = existing.Select(subtask => subtask.Id).ToHashSet();
                var retainedIds = subtasks.Where(subtask => subtask.Id > 0)
                    .Select(subtask => subtask.Id).ToList();

                if (retainedIds.Count != retainedIds.Distinct().Count() ||
                    retainedIds.Any(id => !existingIds.Contains(id)))
                {
                    throw new ArgumentException("Подзадача не принадлежит редактируемой задаче.");
                }

                if (transaction.Update(task) != 1)
                {
                    throw new KeyNotFoundException("Задача больше не существует.");
                }

                foreach (var removed in existing.Where(subtask => !retainedIds.Contains(subtask.Id)))
                {
                    transaction.Delete(removed);
                }

                foreach (var subtask in subtasks)
                {
                    subtask.TaskId = task.Id;
                    if (subtask.Id == 0)
                        transaction.Insert(subtask);
                    else
                        transaction.Update(subtask);
                }
            });
        }
    }
}
