
using ToDoList.Core.Repositories;
using ToDoList.Models;

namespace ToDoList.Services;

public class SubtaskService
{
    private readonly SubtaskRepository _subtaskRepository;
    private readonly TaskRepository _taskRepository;

    public SubtaskService(
        SubtaskRepository subtaskRepository,
        TaskRepository taskRepository)
    {
        _subtaskRepository = subtaskRepository;
        _taskRepository = taskRepository;
    }

    public async Task<List<Subtask>> GetSubtasksByTaskIdAsync(int taskId)
    {
        if (taskId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskId),
                "Идентификатор задачи должен быть положительным.");
        }

        TaskItem? task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null)
        {
            throw new KeyNotFoundException(
                "Основная задача не найдена.");
        }

        return await _subtaskRepository.GetByTaskIdAsync(taskId);
    }


    public async Task<Subtask> CreateSubtaskAsync(int taskId, string name)
    {
        if (taskId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taskId),
                "Идентификатор задачи должен быть положительным.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Название подзадачи не может быть пустым.",
                nameof(name));
        }

        TaskItem? task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null)
        {
            throw new KeyNotFoundException(
                "Основная задача не найдена.");
        }

        Subtask subtask = new()
        {
            TaskId = taskId,
            Name = name.Trim(),
            IsCompleted = false
        };

        await _subtaskRepository.AddAsync(subtask);

        return subtask;
    }

    public async Task<Subtask> UpdateSubtaskAsync(int id, string name)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Идентификатор подзадачи должен быть положительным.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Название подзадачи не может быть пустым.",
                nameof(name));
        }

        Subtask? subtask = await _subtaskRepository.GetByIdAsync(id);

        if (subtask == null)
        {
            throw new KeyNotFoundException(
                "Подзадача не найдена.");
        }

        subtask.Name = name.Trim();

        await _subtaskRepository.UpdateAsync(subtask);

        return subtask;
    }


    public async Task<Subtask> SetSubtaskCompletedAsync(
        int id, bool isCompleted)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Идентификатор подзадачи должен быть положительным.");
        }

        Subtask? subtask = await _subtaskRepository.GetByIdAsync(id);

        if (subtask == null)
        {
            throw new KeyNotFoundException(
                "Подзадача не найдена.");
        }

        subtask.IsCompleted = isCompleted;

        await _subtaskRepository.UpdateAsync(subtask);

        return subtask;
    }


    public async Task<int> DeleteSubtaskAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Идентификатор подзадачи должен быть положительным.");
        }

        Subtask? subtask = await _subtaskRepository.GetByIdAsync(id);

        if (subtask == null)
        {
            throw new KeyNotFoundException(
                "Подзадача не найдена.");
        }

        return await _subtaskRepository.DeleteAsync(id);
    }
}
