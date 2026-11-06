using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToDoList.Models;
using ToDoList.Services;
using ToDoList.Core.Repositories;
using ToDoList.Core.DTOs;

namespace ToDoList.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly TaskService _taskService;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    private readonly CategoryRepository _categoryRepository;

    public ObservableCollection<Category> Categories { get; } = new();

    public bool IsEmpty => Tasks.Count == 0;

    public bool HasTasks => Tasks.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        TaskService taskService,
        CategoryRepository categoryRepository)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;

        Tasks.CollectionChanged += OnTasksChanged;
    }

    public async Task LoadCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        Categories.Clear();

        Categories.Add(new Category
        {
            Id = 0,
            Name = "Все",
            Color = "#6546DF"
        });

        foreach (var category in categories)
        {
            Categories.Add(category);
        }
    }

    public async Task LoadTasksAsync()
    {
        // Получаем задачи, которые уже есть в БД.
        var tasks = await _taskService.GetAllTasksAsync();

        // Если БД пустая — создаём тестовые задачи.
        if (tasks.Count == 0)
        {
            await _taskService.CreateTaskAsync(new CreateTaskRequest
            {
                Name = "Купить продукты",
                CategoryId = 4,
                DueDate = DateTime.Today,
                Subtasks = []
            });

            await _taskService.CreateTaskAsync(new CreateTaskRequest
            {
                Name = "Закончить главный экран приложения",
                CategoryId = 2,
                DueDate = DateTime.Today.AddDays(1),
                Subtasks = []
            });

            await _taskService.CreateTaskAsync(new CreateTaskRequest
            {
                Name = "Очень длинное название задачи для проверки отображения текста в карточке",
                CategoryId = 3,
                DueDate = null,
                Subtasks = []
            });

            // Читаем из БД уже созданные задачи.
            tasks = await _taskService.GetAllTasksAsync();
        }

        // Обновляем коллекцию, которую показывает экран.
        var categories = await _categoryRepository.GetAllAsync();

        Tasks.Clear();

        foreach (var task in tasks)
        {
            task.Category = categories.FirstOrDefault(
                category => category.Id == task.CategoryId);

            Tasks.Add(task);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private void OnTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasTasks));
        }
}
