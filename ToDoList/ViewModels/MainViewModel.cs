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
using ToDoList.Navigation;

namespace ToDoList.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly TaskService _taskService;
    private readonly CategoryRepository _categoryRepository;
    private readonly INavigationService _navigation;
    private readonly HashSet<int> _tasksUpdatingCompletion = new();
    private TaskCompletionSource? _completionFinished;
    private bool _isNavigating;
    private bool _isRefreshing;
    private string _errorMessage = "";


    private readonly List<TaskItem> _allTasks = new();


    private int _selectedCategoryId;


    private TaskItem? _taskPendingDeletion;
    private bool _isDeleting;
    private bool _isDeleteConfirmationVisible;
    private string _deleteConfirmationMessage = "";


    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public ObservableCollection<Category> Categories { get; } = new();


    public ICommand SelectCategoryCommand { get; }
    public Command CreateTaskCommand { get; }
    public Command<TaskItem> OpenTaskCommand { get; }
    public Command<TaskItem> EditTaskCommand { get; }
    public Command<TaskItem> ToggleTaskCompletionCommand { get; }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public Command<TaskItem> RequestDeleteTaskCommand { get; }

    public Command ConfirmDeleteTaskCommand { get; }

    public Command CancelDeleteTaskCommand { get; }


    public int SelectedCategoryId => _selectedCategoryId;


    public bool IsEmpty => Tasks.Count == 0;

    public bool HasTasks => Tasks.Count > 0;

    public string EmptyMessage => _selectedCategoryId == 0
        ? "Задач пока нет"
        : "В этой категории пока нет задач";


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


    public bool IsMainContentEnabled => !IsDeleteConfirmationVisible;


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
        CategoryRepository categoryRepository,
        INavigationService navigation)
    {
        _taskService = taskService;
        _categoryRepository = categoryRepository;
        _navigation = navigation;


        CreateTaskCommand = new Command(async () => await CreateTaskAsync());
        OpenTaskCommand = new Command<TaskItem>(async task => await NavigateToTaskAsync(task, false));
        EditTaskCommand = new Command<TaskItem>(async task => await NavigateToTaskAsync(task, true));
        ToggleTaskCompletionCommand = new Command<TaskItem>(async task => await ToggleTaskCompletionAsync(task));

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

    public async Task RefreshAsync()
    {
        if (_isRefreshing) 
        { 
            return; 
        }

        _isRefreshing = true;
        ErrorMessage = "";

        try
        {
            await LoadCategoriesAsync();
            await LoadTasksAsync();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ErrorMessage = "Не удалось загрузить задачи. Откройте страницу повторно.";
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task CreateTaskAsync()
    {
        if (_isNavigating || _isRefreshing || _isDeleting || IsDeleteConfirmationVisible ||
            _tasksUpdatingCompletion.Count > 0)
        {
            return;
        }

        _isNavigating = true;
        ErrorMessage = "";

        try
        {
            await _navigation.OpenTaskEditorAsync();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ErrorMessage = "Не удалось открыть создание задачи. Попробуйте ещё раз.";
        }
        finally
        {
            _isNavigating = false;
        }
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


    private async Task NavigateToTaskAsync(TaskItem? task, bool edit)
    {
        if (task == null || _isNavigating || _isDeleting || IsDeleteConfirmationVisible ||
            !_allTasks.Any(item => item.Id == task.Id))
        {
            return;
        }

        _isNavigating = true;
        ErrorMessage = "";

        try
        {
            if (_completionFinished != null)
            {
                await _completionFinished.Task;
            }

            if (edit)
                await _navigation.OpenTaskEditorAsync(task.Id);
            else
                await _navigation.OpenTaskDetailsAsync(task.Id);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            ErrorMessage = "Не удалось открыть задачу. Попробуйте ещё раз.";
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async Task ToggleTaskCompletionAsync(TaskItem? task)
    {
        if (task == null || _isNavigating || _isDeleting || IsDeleteConfirmationVisible ||
            !_allTasks.Any(item => item.Id == task.Id))
        {
            return;
        }

        if (!_tasksUpdatingCompletion.Add(task.Id)) return;

        if (_tasksUpdatingCompletion.Count == 1)
        {
            _completionFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        bool previousValue = task.IsCompleted;
        ErrorMessage = "";

        task.IsCompleted = !previousValue;
        try
        {
            await _taskService.SetTaskCompletedAsync(task.Id, task.IsCompleted);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            task.IsCompleted = previousValue;
            ErrorMessage = "Не удалось сохранить отметку выполнения. Попробуйте ещё раз.";
        }
        finally
        {
            _tasksUpdatingCompletion.Remove(task.Id);
            if (_tasksUpdatingCompletion.Count == 0)
            {
                _completionFinished?.TrySetResult();
            }
        }
    }



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

    private void ApplyCategoryFilter()
    {
        var filteredTasks = _allTasks.Where(task =>
                _selectedCategoryId == 0 ||
                task.CategoryId == _selectedCategoryId).ToList();

        for (int i = Tasks.Count - 1; i >= 0; i--)
        {
            if (!filteredTasks.Contains(Tasks[i]))
            {
                Tasks.RemoveAt(i);
            }
        }

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



    private void RequestDeleteTask(TaskItem? task)
    {
        if (task == null || _isNavigating || _isDeleting || IsDeleteConfirmationVisible ||
            !_allTasks.Any(item => item.Id == task.Id) || _tasksUpdatingCompletion.Contains(task.Id))
        {
            return;
        }

        _taskPendingDeletion = task;

        DeleteConfirmationMessage =
            $"Вы уверены, что хотите удалить задачу «{task.Name}»?";

        IsDeleteConfirmationVisible = true;
    }



    private void CancelDeleteTask()
    {
        if (_isDeleting)
        {
            return;
        }

        CloseDeleteConfirmation();
    }



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

            await _taskService.DeleteTaskAsync(task.Id);


            _allTasks.RemoveAll(item => item.Id == task.Id);


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



    private void SetDeleting(bool value)
    {
        _isDeleting = value;

        ConfirmDeleteTaskCommand.ChangeCanExecute();
        CancelDeleteTaskCommand.ChangeCanExecute();
    }



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
