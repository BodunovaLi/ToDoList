
using ToDoList.Core.DTOs;
using ToDoList.Core.Repositories;
using ToDoList.Data;
using ToDoList.Models;
using ToDoList.Services;

namespace ToDoList.Tests;

public class SubtaskServiceTests : IAsyncLifetime
{
    private readonly string _databasePath;

    private readonly AppDatabase _database;
    private readonly TaskRepository _taskRepository;
    private readonly SubtaskRepository _subtaskRepository;
    private readonly CategoryRepository _categoryRepository;

    private readonly TaskService _taskService;
    private readonly SubtaskService _subtaskService;

    public SubtaskServiceTests()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"subtask_tests_{Guid.NewGuid():N}.db");

        _database = new AppDatabase(_databasePath);

        _taskRepository = new TaskRepository(_database);
        _subtaskRepository = new SubtaskRepository(_database);
        _categoryRepository = new CategoryRepository(_database);

        _taskService = new TaskService(
            _taskRepository,
            _categoryRepository);

        _subtaskService = new SubtaskService(
            _subtaskRepository,
            _taskRepository);
    }

    public async Task InitializeAsync()
    {
        await _database.GetConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.CloseAsync();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    // Вспомогательный метод создания основной задачи
    private async Task<TaskItem> CreateTestTaskAsync(
        string name = "Тестовая задача")
    {
        return await _taskService.CreateTaskAsync(
            new CreateTaskRequest
            {
                Name = name,
                CategoryId = 5
            });
    }

    #region GetSubtasksByTaskIdAsync

    // 1. Получение существующих подзадач
    [Fact]
    public async Task GetSubtasksByTaskIdAsync_ExistingSubtasks_ShouldReturnList()
    {
        var task = await CreateTestTaskAsync();

        await _subtaskService.CreateSubtaskAsync(task.Id, "Первая");
        await _subtaskService.CreateSubtaskAsync(task.Id, "Вторая");

        var subtasks =
            await _subtaskService.GetSubtasksByTaskIdAsync(task.Id);

        Assert.Equal(2, subtasks.Count);
        Assert.Contains(subtasks, s => s.Name == "Первая");
        Assert.Contains(subtasks, s => s.Name == "Вторая");
    }

    // 2. Задача без подзадач
    [Fact]
    public async Task GetSubtasksByTaskIdAsync_NoSubtasks_ShouldReturnEmptyList()
    {
        var task = await CreateTestTaskAsync();

        var subtasks =
            await _subtaskService.GetSubtasksByTaskIdAsync(task.Id);

        Assert.Empty(subtasks);
    }

    // 3. Подзадачи разных задач не смешиваются
    [Fact]
    public async Task GetSubtasksByTaskIdAsync_DifferentTasks_ShouldReturnOnlyOwnSubtasks()
    {
        var firstTask = await CreateTestTaskAsync("Первая задача");
        var secondTask = await CreateTestTaskAsync("Вторая задача");

        await _subtaskService.CreateSubtaskAsync(firstTask.Id, "А");
        await _subtaskService.CreateSubtaskAsync(secondTask.Id, "Б");

        var subtasks =
            await _subtaskService.GetSubtasksByTaskIdAsync(firstTask.Id);

        Assert.Single(subtasks);
        Assert.Equal("А", subtasks[0].Name);
        Assert.Equal(firstTask.Id, subtasks[0].TaskId);
    }

    // 4. Некорректный ID
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetSubtasksByTaskIdAsync_InvalidId_ShouldThrow(int id)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _subtaskService.GetSubtasksByTaskIdAsync(id));
    }

    // 5. Основная задача не существует
    [Fact]
    public async Task GetSubtasksByTaskIdAsync_NonExistingTask_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _subtaskService.GetSubtasksByTaskIdAsync(99999));
    }

    #endregion

    #region CreateSubtaskAsync

    // 6. Успешное создание подзадачи
    [Fact]
    public async Task CreateSubtaskAsync_ValidData_ShouldCreateSubtask()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Купить молоко");

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.True(subtask.Id > 0);
        Assert.Equal("Купить молоко", saved.Name);
        Assert.Equal(task.Id, saved.TaskId);
    }

    // 7. Новая подзадача не выполнена
    [Fact]
    public async Task CreateSubtaskAsync_ShouldBeNotCompletedByDefault()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Подзадача");

        Assert.False(subtask.IsCompleted);
    }

    // 8. Обрезка пробелов
    [Fact]
    public async Task CreateSubtaskAsync_NameWithSpaces_ShouldTrimName()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(
                task.Id, "   Купить хлеб   ");

        Assert.Equal("Купить хлеб", subtask.Name);
    }

    // 9. Некорректный ID основной задачи
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateSubtaskAsync_InvalidTaskId_ShouldThrow(int id)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _subtaskService.CreateSubtaskAsync(id, "Подзадача"));
    }

    // 10. Основная задача не существует
    [Fact]
    public async Task CreateSubtaskAsync_NonExistingTask_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _subtaskService.CreateSubtaskAsync(99999, "Подзадача"));
    }

    // 11. Пустое название
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateSubtaskAsync_EmptyName_ShouldThrow(string? name)
    {
        var task = await CreateTestTaskAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _subtaskService.CreateSubtaskAsync(task.Id, name!));

        var subtasks =
            await _subtaskService.GetSubtasksByTaskIdAsync(task.Id);

        Assert.Empty(subtasks);
    }

    #endregion

    #region UpdateSubtaskAsync

    // 12. Успешное изменение названия
    [Fact]
    public async Task UpdateSubtaskAsync_ValidName_ShouldUpdateName()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Старое");

        await _subtaskService.UpdateSubtaskAsync(
            subtask.Id, "Новое");

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.Equal("Новое", saved.Name);
    }

    // 13. Обрезка пробелов
    [Fact]
    public async Task UpdateSubtaskAsync_NameWithSpaces_ShouldTrimName()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Старое");

        var updated =
            await _subtaskService.UpdateSubtaskAsync(
                subtask.Id, "   Новое   ");

        Assert.Equal("Новое", updated.Name);
    }

    // 14. Сохранение ID, TaskId и статуса
    [Fact]
    public async Task UpdateSubtaskAsync_ShouldPreserveOtherProperties()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Старое");

        await _subtaskService.SetSubtaskCompletedAsync(
            subtask.Id, true);

        await _subtaskService.UpdateSubtaskAsync(
            subtask.Id, "Новое");

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.Equal(subtask.Id, saved.Id);
        Assert.Equal(task.Id, saved.TaskId);
        Assert.True(saved.IsCompleted);
    }

    // 15. Некорректный ID
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateSubtaskAsync_InvalidId_ShouldThrow(int id)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _subtaskService.UpdateSubtaskAsync(id, "Новое"));
    }

    // 16. Подзадача не существует
    [Fact]
    public async Task UpdateSubtaskAsync_NonExistingSubtask_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _subtaskService.UpdateSubtaskAsync(99999, "Новое"));
    }

    // 17. Пустое название
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateSubtaskAsync_EmptyName_ShouldThrow(string? name)
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Старое");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _subtaskService.UpdateSubtaskAsync(subtask.Id, name!));

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.Equal("Старое", saved.Name);
    }

    #endregion

    #region SetSubtaskCompletedAsync

    // 18. Отметка выполнения
    [Fact]
    public async Task SetSubtaskCompletedAsync_True_ShouldMarkCompleted()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Подзадача");

        await _subtaskService.SetSubtaskCompletedAsync(
            subtask.Id, true);

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.True(saved.IsCompleted);
    }

    // 19. Отмена выполнения
    [Fact]
    public async Task SetSubtaskCompletedAsync_False_ShouldMarkNotCompleted()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Подзадача");

        await _subtaskService.SetSubtaskCompletedAsync(
            subtask.Id, true);

        await _subtaskService.SetSubtaskCompletedAsync(
            subtask.Id, false);

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.False(saved.IsCompleted);
    }

    // 20. Остальные свойства не изменяются
    [Fact]
    public async Task SetSubtaskCompletedAsync_ShouldPreserveOtherProperties()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Подзадача");

        await _subtaskService.SetSubtaskCompletedAsync(
            subtask.Id, true);

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.NotNull(saved);
        Assert.Equal(subtask.Id, saved.Id);
        Assert.Equal(task.Id, saved.TaskId);
        Assert.Equal("Подзадача", saved.Name);
    }

    // 21. Некорректный ID
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SetSubtaskCompletedAsync_InvalidId_ShouldThrow(int id)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _subtaskService.SetSubtaskCompletedAsync(id, true));
    }

    // 22. Подзадача не существует
    [Fact]
    public async Task SetSubtaskCompletedAsync_NonExistingSubtask_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _subtaskService.SetSubtaskCompletedAsync(99999, true));
    }

    #endregion

    #region DeleteSubtaskAsync

    // 23. Успешное удаление
    [Fact]
    public async Task DeleteSubtaskAsync_ExistingSubtask_ShouldDelete()
    {
        var task = await CreateTestTaskAsync();

        var subtask =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Подзадача");

        int deleted =
            await _subtaskService.DeleteSubtaskAsync(subtask.Id);

        var saved =
            await _subtaskRepository.GetByIdAsync(subtask.Id);

        Assert.Equal(1, deleted);
        Assert.Null(saved);
    }

    // 24. Остальные подзадачи сохраняются
    [Fact]
    public async Task DeleteSubtaskAsync_ShouldPreserveOtherSubtasks()
    {
        var task = await CreateTestTaskAsync();

        var first =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Первая");

        var second =
            await _subtaskService.CreateSubtaskAsync(task.Id, "Вторая");

        await _subtaskService.DeleteSubtaskAsync(first.Id);

        var subtasks =
            await _subtaskService.GetSubtasksByTaskIdAsync(task.Id);

        Assert.Single(subtasks);
        Assert.Equal(second.Id, subtasks[0].Id);
    }

    // 25. Некорректный ID
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteSubtaskAsync_InvalidId_ShouldThrow(int id)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _subtaskService.DeleteSubtaskAsync(id));
    }

    // 26. Подзадача не существует
    [Fact]
    public async Task DeleteSubtaskAsync_NonExistingSubtask_ShouldThrow()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _subtaskService.DeleteSubtaskAsync(99999));
    }

    #endregion
}
