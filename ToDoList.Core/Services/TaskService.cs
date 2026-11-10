using System;
using System.Collections.Generic;
using System.Text;
using ToDoList.Core.Repositories;
using ToDoList.Data;
using ToDoList.Models;
using ToDoList.Core.DTOs;

namespace ToDoList.Services
{
    public class TaskService
    {
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;

        public TaskService(TaskRepository taskRepository, CategoryRepository categoryRepository)
        {
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await _taskRepository.GetAllAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Идентификатор задачи должен быть положительным."
                );
            }

            return await _taskRepository.GetByIdAsync(id);
        }

        public async Task<int> DeleteTaskAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Идентификатор задачи должен быть положительным."
                );
            }

            return await _taskRepository.DeleteAsync(id);
        }


        public async Task<TaskItem> CreateTaskAsync(CreateTaskRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException(
                    "Название задачи не может быть пустым.",
                    nameof(request));
            }

            if (request.Subtasks == null ||
                request.Subtasks.Any(s =>
                    s == null || string.IsNullOrWhiteSpace(s.Name)))
            {
                throw new ArgumentException(
                    "Названия подзадач не могут быть пустыми.",
                    nameof(request));
            }


            if (request.DueDate.HasValue &&
                request.DueDate.Value.Date < DateTime.Today)
            {
                throw new ArgumentException(
                    "Дата выполнения не может быть раньше сегодняшней.",
                    nameof(request));
            }

            int categoryId = request.CategoryId ?? 5;

            List<Category> categories = await _categoryRepository.GetAllAsync();

            if (!categories.Any(c => c.Id == categoryId))
            {
                throw new ArgumentException(
                    "Указанная категория не существует.",
                    nameof(request));
            }


            TaskItem task = new TaskItem
            {
                Name = request.Name.Trim(),
                CategoryId = categoryId,
                IsCompleted = false,
                CreatedAt = DateTime.Now,
                DueDate = request.DueDate?.Date
            };

            List<Subtask> subtasks = request.Subtasks.Select(draft => new Subtask
                {
                    Name = draft.Name.Trim(),
                    IsCompleted = draft.IsCompleted
                }).ToList();



            await _taskRepository.AddWithSubtasksAsync(task, subtasks);

            return task;
        }


        public async Task<TaskItem> UpdateTaskAsync(UpdateTaskRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "Идентификатор задачи должен быть положительным.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException(
                    "Название задачи не может быть пустым.",
                    nameof(request));
            }

            TaskItem? task = await _taskRepository.GetByIdAsync(request.Id);

            if (task == null)
            {
                throw new KeyNotFoundException(
                    "Задача с указанным идентификатором не найдена.");
            }

            List<Category> categories = await _categoryRepository.GetAllAsync();

            if (!categories.Any(c => c.Id == request.CategoryId))
            {
                throw new ArgumentException(
                    "Указанная категория не существует.",
                    nameof(request));
            }

            if (request.DueDate.HasValue &&
                request.DueDate.Value.Date < DateTime.Today &&
                request.DueDate.Value.Date != task.DueDate?.Date)
            {
                throw new ArgumentException(
                    "Нельзя установить новую дату выполнения в прошлом.",
                    nameof(request));
            }

            task.Name = request.Name.Trim();
            task.CategoryId = request.CategoryId;
            task.DueDate = request.DueDate?.Date;

            if (request.Subtasks == null)
            {
                await _taskRepository.UpdateAsync(task);
            }
            else
            {
                if (request.Subtasks.Any(subtask => subtask == null ||
                    subtask.Id < 0 || string.IsNullOrWhiteSpace(subtask.Name)))
                {
                    throw new ArgumentException("Проверьте названия и идентификаторы подзадач.", nameof(request));
                }

                await _taskRepository.UpdateWithSubtasksAsync(task,
                    request.Subtasks.Select(subtask => new Subtask
                    {
                        Id = subtask.Id,
                        Name = subtask.Name.Trim(),
                        IsCompleted = subtask.IsCompleted
                    }).ToList());
            }

            return task;
        }


        public async Task<TaskItem> SetTaskCompletedAsync(int id, bool isCompleted)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Идентификатор задачи должен быть положительным.");
            }

            TaskItem? task = await _taskRepository.GetByIdAsync(id);

            if (task == null)
            {
                throw new KeyNotFoundException(
                    "Задача с указанным идентификатором не найдена.");
            }

            task.IsCompleted = isCompleted;

            await _taskRepository.UpdateAsync(task);

            return task;
        }



    }
}
