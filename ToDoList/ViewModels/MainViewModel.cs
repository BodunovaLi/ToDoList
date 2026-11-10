using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using ToDoList.Core.Repositories;
using ToDoList.Models;
using ToDoList.Services;

namespace ToDoList.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly TaskService _taskService;
    private readonly CategoryRepository _categoryRepository;

    // Все загруженные задачи.
    private readonly List<TaskItem> _allTasks = new();

    // Выбранная категория: 0 означает «Все».
    private int _selectedCategoryId;

    // Состояние подтверждения удаления.
    private TaskItem? _taskPendingDeletion;
    private bool _isDeleting;
    private bool _isDeleteConfirmationVisible;
    private string _deleteConfirmationMessage = "";

    // Коллекции для отображения.
    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public ObservableCollection<Category> Categories { get; } = new();

    // Команды.
    public ICommand SelectCategoryCommand { get; }

    public Command<TaskItem> RequestDeleteTaskCommand { get; }

    public Command ConfirmDeleteTaskCommand { get; }

    public Command CancelDeleteTaskCommand { get; }

    // Выбранная категория.
    public int SelectedCategoryId => _selectedCategoryId;

    // Пустое состояние.
    public bool IsEmpty => Tasks.Count == 0;

    public bool HasTasks => Tasks.Count > 0;

    public string EmptyMessage => _selectedCategoryId == 0
        ? "Задач пока нет"
        : "В этой категории пока нет задач";

    // Видимость диалога.
    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        private set
        {
            if (_isDeleteConfirmationVisible == value)
            {
                return;
            }

            _isDeleteConfirmationVisible = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsMainContentEnabled));
        }
    }

    // Блокируем основной интерфейс, пока открыт диалог.
    public bool IsMainContentEnabled => !IsDeleteConfirmationVisible;

    // Вопрос или сообщение об ошибке в диалоге.
    public string DeleteConfirmationMessage
    {
        get => _deleteConfirmationMessage;
        private set
        {
            if (_deleteConfirmationMessage == value)
            {
                return;
            }

            _deleteConfirmationMessage = value;

            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        TaskService taskService,
        CategoryRepository categoryRepository)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;

        SelectCategoryCommand =
            new Command<Category>(SelectCategory);

        RequestDeleteTaskCommand =
            new Command<TaskItem>(RequestDeleteTask);

        ConfirmDeleteTaskCommand = new Command(
            async () => await ConfirmDeleteTaskAsync(),
            () => !_isDeleting);

        CancelDeleteTaskCommand = new Command(
            CancelDeleteTask,
            () => !_isDeleting);

        Tasks.CollectionChanged += OnTasksChanged;
    }

    // Загрузка категорий.

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

    // Загрузка задач.

    public async Task LoadTasksAsync()
    {
        const string seedKey = "SampleTaskCreated_v1";

        if (!Microsoft.Maui.Storage.Preferences.Default.Get(seedKey, false))
        {
            await _taskService.CreateTaskAsync(new ToDoList.Core.DTOs.CreateTaskRequest
            {
                Name = "Закончить главный экран приложения",
                CategoryId = 2,
                DueDate = DateTime.Today
            });

            Microsoft.Maui.Storage.Preferences.Default.Set(seedKey, true);
        }

        var tasks = await _taskService.GetAllTasksAsync();
        var categories = await _categoryRepository.GetAllAsync();

        foreach (var task in tasks)
        {
            task.Category = categories.FirstOrDefault(
                category => category.Id == task.CategoryId);
        }

        _allTasks.Clear();
        _allTasks.AddRange(tasks);

        ApplyCategoryFilter();
    }

    public async Task SetTaskCompletedAsync(TaskItem task, bool isCompleted)
    {
        await _taskService.SetTaskCompletedAsync(task.Id, isCompleted);
        task.IsCompleted = isCompleted;
    }

    // Выбор категории.

    private void SelectCategory(Category? category)
    {
        if (category == null ||
            IsDeleteConfirmationVisible ||
            category.Id == _selectedCategoryId)
        {
            return;
        }

        _selectedCategoryId = category.Id;

        OnPropertyChanged(nameof(SelectedCategoryId));
        OnPropertyChanged(nameof(EmptyMessage));

        ApplyCategoryFilter();
    }

    // Обновление видимого списка.

    private void ApplyCategoryFilter()
    {
        var filteredTasks = _allTasks
            .Where(task =>
                _selectedCategoryId == 0 ||
                task.CategoryId == _selectedCategoryId)
            .ToList();

        // Убираем задачи, которые больше не должны отображаться.
        for (int i = Tasks.Count - 1; i >= 0; i--)
        {
            if (!filteredTasks.Contains(Tasks[i]))
            {
                Tasks.RemoveAt(i);
            }
        }

        // Добавляем недостающие задачи и сохраняем порядок.
        for (int i = 0; i < filteredTasks.Count; i++)
        {
            var task = filteredTasks[i];
            int currentIndex = Tasks.IndexOf(task);

            if (currentIndex == -1)
            {
                Tasks.Insert(i, task);
            }
            else if (currentIndex != i)
            {
                Tasks.Move(currentIndex, i);
            }
        }
    }

    // Открытие подтверждения. Здесь задача ещё не удаляется.

    private void RequestDeleteTask(TaskItem? task)
    {
        if (task == null ||
            _isDeleting ||
            IsDeleteConfirmationVisible)
        {
            return;
        }

        _taskPendingDeletion = task;

        DeleteConfirmationMessage =
            $"Вы уверены, что хотите удалить задачу «{task.Name}»?";

        IsDeleteConfirmationVisible = true;
    }

    // Отмена удаления.

    private void CancelDeleteTask()
    {
        if (_isDeleting)
        {
            return;
        }

        CloseDeleteConfirmation();
    }

    // Удаление после подтверждения.

    private async Task ConfirmDeleteTaskAsync()
    {
        if (_taskPendingDeletion == null ||
            !IsDeleteConfirmationVisible ||
            _isDeleting)
        {
            return;
        }

        var task = _taskPendingDeletion;

        SetDeleting(true);

        try
        {
            // Сначала удаляем запись из БД.
            await _taskService.DeleteTaskAsync(task.Id);

            // Затем обновляем загруженные данные.
            _allTasks.RemoveAll(item => item.Id == task.Id);

            // Фильтр уберёт удалённую задачу из видимого списка.
            ApplyCategoryFilter();

            CloseDeleteConfirmation();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);

            DeleteConfirmationMessage =
                "Не удалось удалить задачу. Повторить попытку?";
        }
        finally
        {
            SetDeleting(false);
        }
    }

    private void CloseDeleteConfirmation()
    {
        IsDeleteConfirmationVisible = false;
        _taskPendingDeletion = null;
        DeleteConfirmationMessage = "";
    }

    // Обновление доступности кнопок диалога.

    private void SetDeleting(bool value)
    {
        _isDeleting = value;

        ConfirmDeleteTaskCommand.ChangeCanExecute();
        CancelDeleteTaskCommand.ChangeCanExecute();
    }

    // Обновление пустого состояния при изменении списка.

    private void OnTasksChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasTasks));
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }


}
