using System.Collections.ObjectModel;
using System.ComponentModel;
using ToDoList.Core.Repositories;
using ToDoList.Models;
using ToDoList.Services;
using ToDoList.Navigation;

namespace ToDoList.ViewModels;

public class TaskDetailsViewModel : IQueryAttributable, INotifyPropertyChanged
{
    private readonly TaskService _tasks;
    private readonly SubtaskService _subtasks;
    private readonly CategoryRepository _categories;
    private readonly INavigationService _navigation;
    private Subtask? _editingSubtask;
    private string _subtaskName = "";
    private int _taskId;
    private TaskItem? _task;
    private bool _isBusy;
    private bool _isSubtaskDialogOpen;
    private bool _isDeleteConfirmationVisible;
    private bool _isDeleted;
    private Subtask? _subtaskPendingDeletion;
    private string _errorMessage = "";

    public ObservableCollection<Subtask> Subtasks { get; } = new();
    public TaskItem? CurrentTask => _task;
    public string Name => _task?.Name ?? "Задача";
    public Category? Category => _task?.Category;
    public string DueDateText => _task?.DueDate?.ToString("dd.MM.yyyy") ?? "Не указана";
    public bool IsCompleted => _task?.IsCompleted == true;
    public string CompletionButtonText => IsCompleted ? "Вернуть в работу" : "✓  Отметить выполнение";
    public string SubtaskProgress => $"{Subtasks.Count(item => item.IsCompleted)}/{Subtasks.Count}";
    public bool HasNoSubtasks => Subtasks.Count == 0;
    public bool IsBusy => _isBusy;
    public bool IsInitialLoading => _isBusy && !HasTask;
    public bool IsDialogEnabled => !_isBusy;
    public bool HasTask => _task != null && !_isDeleted;
    public bool IsContentEnabled => HasTask && !_isBusy && !IsSubtaskDialogOpen && !IsDeleteConfirmationVisible;
    public bool IsContentBlocked => !IsContentEnabled;
    public string ErrorMessage => _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);
    public bool HasPageError => HasError && !IsSubtaskDialogOpen;
    public string DeleteConfirmationMessage { get; private set; } = "";
    public string SubtaskDialogTitle => _editingSubtask == null ? "Добавить подзадачу" : "Редактирование подзадачи";
    public string SubtaskConfirmText => _editingSubtask == null ? "Добавить" : "Сохранить";

    public string SubtaskName
    {
        get => _subtaskName;
        set
        {
            if (_subtaskName == value) return;
            _subtaskName = value;
            Notify(nameof(SubtaskName));
            ConfirmSubtaskCommand.ChangeCanExecute();
        }
    }

    public bool IsSubtaskDialogOpen
    {
        get => _isSubtaskDialogOpen;
        private set
        {
            if (_isSubtaskDialogOpen == value) return;
            _isSubtaskDialogOpen = value;
            Notify(nameof(IsSubtaskDialogOpen), nameof(HasPageError));
            UpdateAvailability();
        }
    }

    public bool IsDeleteConfirmationVisible => _isDeleteConfirmationVisible;
    public Command BackCommand { get; }
    public Command EditCommand { get; }
    public Command ToggleCompletionCommand { get; }
    public Command RequestDeleteCommand { get; }
    public Command ConfirmDeleteCommand { get; }
    public Command CancelDeleteCommand { get; }
    public Command RetryCommand { get; }
    public Command AddSubtaskCommand { get; }
    public Command<Subtask> EditSubtaskCommand { get; }
    public Command ConfirmSubtaskCommand { get; }
    public Command CancelSubtaskCommand { get; }
    public Command<Subtask> RequestDeleteSubtaskCommand { get; }
    public Command<Subtask> ToggleSubtaskCompletionCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public TaskDetailsViewModel(TaskService tasks, SubtaskService subtasks, CategoryRepository categories,
        INavigationService navigation)
    {
        _tasks = tasks;
        _subtasks = subtasks;
        _categories = categories;
        _navigation = navigation;
        BackCommand = new Command(async () => await RunAsync(() => _navigation.GoBackAsync()),
            () => !_isBusy && !IsSubtaskDialogOpen && !IsDeleteConfirmationVisible);
        EditCommand = new Command(async () => await RunAsync(() =>
            _navigation.OpenTaskEditorAsync(_taskId)), () => IsContentEnabled);
        ToggleCompletionCommand = new Command(async () => await RunAsync(async () =>
        {
            var saved = await _tasks.SetTaskCompletedAsync(_taskId, !IsCompleted);
            _task!.IsCompleted = saved.IsCompleted;
            Notify(nameof(IsCompleted), nameof(CompletionButtonText));
        }), () => IsContentEnabled);
        RequestDeleteCommand = new Command(() => RequestDelete(null), () => IsContentEnabled);
        ConfirmDeleteCommand = new Command(async () => await ConfirmDeleteAsync(),
            () => !_isBusy && IsDeleteConfirmationVisible);
        CancelDeleteCommand = new Command(CloseDeleteConfirmation, () => !_isBusy);
        RetryCommand = new Command(async () => await LoadAsync(), () => !_isBusy && !_isDeleted);
        AddSubtaskCommand = new Command(() => OpenSubtaskDialog(null), () => IsContentEnabled);
        EditSubtaskCommand = new Command<Subtask>(OpenSubtaskDialog,
            subtask => IsContentEnabled && subtask != null && Subtasks.Contains(subtask));
        ConfirmSubtaskCommand = new Command(async () => await ConfirmSubtaskAsync(),
            () => IsSubtaskDialogOpen && !_isBusy && !string.IsNullOrWhiteSpace(SubtaskName));
        CancelSubtaskCommand = new Command(CloseSubtaskDialog, () => IsSubtaskDialogOpen && !_isBusy);
        RequestDeleteSubtaskCommand = new Command<Subtask>(RequestDeleteSubtask,
            subtask => IsContentEnabled && subtask != null && Subtasks.Contains(subtask));
        ToggleSubtaskCompletionCommand = new Command<Subtask>(async subtask => await ToggleSubtaskCompletionAsync(subtask),
            subtask => IsContentEnabled && subtask != null && Subtasks.Contains(subtask));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _taskId = query.TryGetValue("taskId", out var value) &&
            int.TryParse(value?.ToString(), out var id) ? id : 0;
    }


    public Task<bool> LoadAsync() => RunAsync(async () =>
    {
        var task = await _tasks.GetTaskByIdAsync(_taskId);
        if (task == null)
        {
            SetTask(null);
            throw new KeyNotFoundException("Задача больше не существует. Вернитесь к списку.");
        }
        var categories = await _categories.GetAllAsync();
        var subtasks = await _subtasks.GetSubtasksByTaskIdAsync(_taskId);
        task.Category = categories.FirstOrDefault(category => category.Id == task.CategoryId);
        SynchronizeSubtasks(subtasks);
        SetTask(task);
    });

    public Task<bool> SaveSubtaskAsync(Subtask? editing, string name) => RunAsync(async () =>
    {
        if (editing == null)
        {
            Subtasks.Add(await _subtasks.CreateSubtaskAsync(_taskId, name));
            NotifySubtaskProgress();
        }
        else
            ReplaceSubtask(await _subtasks.UpdateSubtaskAsync(editing.Id, name));
    });

    private async Task ToggleSubtaskCompletionAsync(Subtask? subtask)
    {
        if (!IsContentEnabled || subtask == null || !Subtasks.Contains(subtask)) return;
        bool previousValue = subtask.IsCompleted;
        await RunAsync(async () =>
        {

            subtask.IsCompleted = !previousValue;
            NotifySubtaskProgress();
            try
            {
                await _subtasks.SetSubtaskCompletedAsync(subtask.Id, subtask.IsCompleted);
            }
            catch
            {
                subtask.IsCompleted = previousValue;
                NotifySubtaskProgress();
                throw;
            }
        });
    }

    private void OpenSubtaskDialog(Subtask? subtask)
    {
        if (!IsContentEnabled || (subtask != null && !Subtasks.Contains(subtask))) return;
        _editingSubtask = subtask;
        SubtaskName = subtask?.Name ?? "";
        SetErrorMessage("");
        Notify(nameof(SubtaskDialogTitle), nameof(SubtaskConfirmText));
        IsSubtaskDialogOpen = true;
    }

    private async Task ConfirmSubtaskAsync()
    {
        if (!IsSubtaskDialogOpen || _isBusy || string.IsNullOrWhiteSpace(SubtaskName)) return;
        if (await SaveSubtaskAsync(_editingSubtask, SubtaskName))
            CloseSubtaskDialog();
    }

    private void CloseSubtaskDialog()
    {
        if (_isBusy) return;
        _editingSubtask = null;
        SubtaskName = "";
        IsSubtaskDialogOpen = false;
        Notify(nameof(SubtaskDialogTitle), nameof(SubtaskConfirmText));
    }

    public void RequestDeleteSubtask(Subtask? subtask)
    {
        if (IsContentEnabled && subtask != null && Subtasks.Contains(subtask)) RequestDelete(subtask);
    }

    private void ReplaceSubtask(Subtask saved)
    {
        var previous = Subtasks.FirstOrDefault(item => item.Id == saved.Id);
        if (previous == null) return;
        bool completionChanged = previous.IsCompleted != saved.IsCompleted;
        previous.Name = saved.Name;
        previous.IsCompleted = saved.IsCompleted;
        if (completionChanged) NotifySubtaskProgress();
    }

    private void SynchronizeSubtasks(List<Subtask> loaded)
    {
        string previousProgress = SubtaskProgress;
        bool previouslyEmpty = HasNoSubtasks;
        var loadedIds = loaded.Select(item => item.Id).ToHashSet();
        for (int i = Subtasks.Count - 1; i >= 0; i--)
            if (!loadedIds.Contains(Subtasks[i].Id)) Subtasks.RemoveAt(i);

        for (int i = 0; i < loaded.Count; i++)
        {
            var saved = loaded[i];
            var existing = Subtasks.FirstOrDefault(item => item.Id == saved.Id);
            if (existing == null)
                Subtasks.Insert(i, saved);
            else
            {
                existing.Name = saved.Name;
                existing.IsCompleted = saved.IsCompleted;
                int currentIndex = Subtasks.IndexOf(existing);
                if (currentIndex != i) Subtasks.Move(currentIndex, i);
            }
        }
        if (previousProgress != SubtaskProgress) Notify(nameof(SubtaskProgress));
        if (previouslyEmpty != HasNoSubtasks) Notify(nameof(HasNoSubtasks));
    }

    private void SetTask(TaskItem? task)
    {
        var previous = _task;
        bool previouslyHadTask = HasTask;
        if (task?.Category is { } category && previous?.Category is { } oldCategory &&
            category.Id == oldCategory.Id && category.Name == oldCategory.Name && category.Color == oldCategory.Color)
            task.Category = oldCategory;

        _task = task;
        if (!ReferenceEquals(previous, task)) Notify(nameof(CurrentTask));
        if (previous?.Name != task?.Name) Notify(nameof(Name));
        if (!ReferenceEquals(previous?.Category, task?.Category)) Notify(nameof(Category));
        if (previous?.DueDate != task?.DueDate) Notify(nameof(DueDateText));
        if (previous?.IsCompleted != task?.IsCompleted) Notify(nameof(IsCompleted), nameof(CompletionButtonText));
        if (previouslyHadTask != HasTask) Notify(nameof(HasTask));
    }

    private void NotifySubtaskProgress() => Notify(nameof(SubtaskProgress), nameof(HasNoSubtasks));

    private void RequestDelete(Subtask? subtask)
    {
        _subtaskPendingDeletion = subtask;
        DeleteConfirmationMessage = subtask == null
            ? $"Удалить задачу «{Name}» и все её подзадачи?"
            : $"Удалить подзадачу «{subtask.Name}»?";
        _isDeleteConfirmationVisible = true;
        Notify(nameof(DeleteConfirmationMessage), nameof(IsDeleteConfirmationVisible));
        UpdateAvailability();
    }

    private async Task ConfirmDeleteAsync()
    {
        if (!IsDeleteConfirmationVisible) return;
        await RunAsync(async () =>
        {
            if (_subtaskPendingDeletion is { } subtask)
            {
                await _subtasks.DeleteSubtaskAsync(subtask.Id);
                Subtasks.Remove(subtask);
                NotifySubtaskProgress();
                CloseDeleteConfirmation();
            }
            else
            {
                await _tasks.DeleteTaskAsync(_taskId);
                _isDeleted = true;
                Notify(nameof(HasTask));
                CloseDeleteConfirmation();
                await _navigation.GoBackAsync();
            }
        });
    }

    private void CloseDeleteConfirmation()
    {
        _isDeleteConfirmationVisible = false;
        _subtaskPendingDeletion = null;
        Notify(nameof(IsDeleteConfirmationVisible));
        UpdateAvailability();
    }

    private async Task<bool> RunAsync(Func<Task> action)
    {
        if (_isBusy) return false;
        _isBusy = true;
        SetErrorMessage("");
        UpdateAvailability();
        try
        {
            await action();
            return true;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            SetErrorMessage(exception is KeyNotFoundException or ArgumentException
                ? exception.Message
                : "Не удалось выполнить действие. Попробуйте ещё раз.");
            if (IsDeleteConfirmationVisible)
            {
                DeleteConfirmationMessage = "Не удалось удалить. Повторить попытку?";
                Notify(nameof(DeleteConfirmationMessage));
            }
            return false;
        }
        finally
        {
            _isBusy = false;
            UpdateAvailability();
        }
    }

    private void SetErrorMessage(string message)
    {
        if (_errorMessage == message) return;
        _errorMessage = message;
        Notify(nameof(ErrorMessage), nameof(HasError), nameof(HasPageError));
    }

    private void Notify(params string[] properties)
    {
        foreach (string property in properties)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    private void UpdateAvailability()
    {
        Notify(nameof(IsBusy), nameof(IsInitialLoading), nameof(IsDialogEnabled),
            nameof(IsContentEnabled), nameof(IsContentBlocked));
        BackCommand.ChangeCanExecute();
        EditCommand.ChangeCanExecute();
        ToggleCompletionCommand.ChangeCanExecute();
        RequestDeleteCommand.ChangeCanExecute();
        ConfirmDeleteCommand.ChangeCanExecute();
        CancelDeleteCommand.ChangeCanExecute();
        RetryCommand.ChangeCanExecute();
        AddSubtaskCommand.ChangeCanExecute();
        EditSubtaskCommand.ChangeCanExecute();
        ConfirmSubtaskCommand.ChangeCanExecute();
        CancelSubtaskCommand.ChangeCanExecute();
        RequestDeleteSubtaskCommand.ChangeCanExecute();
        ToggleSubtaskCompletionCommand.ChangeCanExecute();
    }
}
