using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToDoList.Core.DTOs;
using ToDoList.Core.Repositories;
using ToDoList.Models;
using ToDoList.Services;
using ToDoList.Navigation;

namespace ToDoList.ViewModels;

public class TaskEditorViewModel : IQueryAttributable, INotifyPropertyChanged
{
    private readonly CategoryRepository _categoryRepos;
    private readonly TaskService _taskService;
    private readonly SubtaskRepository _subtaskRepository;
    private readonly INavigationService _navigation;
    private Subtask? _editingSubtask;
    private string _subtaskName = "";
    private bool _isNavigating;
    private int _taskId;
    private bool _initialized;
    private bool _isLoading;
    private bool _isSaving;
    private bool _saved;
    private bool _isSubtaskDialogOpen;
    private string _saveErrorMessage = "";
    private string _name = "";
    private DateTime? _plannedDate;
    private DateTime? _originalDueDate;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Subtask> Subtasks { get; } = new();
    public bool IsEditMode { get; private set; }
    public string Header => IsEditMode ? "Редактирование задачи" : "Создать задачу";
    public string SubHeader => IsEditMode ? "Отредактируйте задачу" : "Создайте задачу и добавьте детали";
    public string SaveButtonText => IsEditMode ? "Сохранить изменения" : "Добавить задачу";
    public int SelectedCategoryId { get; private set; } = 5;
    public Command<Category> SelectCategoryCommand { get; }
    public Command SaveTaskCommand { get; }
    public Command BackCommand { get; }
    public Command AddSubtaskCommand { get; }
    public Command<Subtask> EditSubtaskCommand { get; }
    public Command<Subtask> RemoveSubtaskCommand { get; }
    public Command ConfirmSubtaskCommand { get; }
    public Command CancelSubtaskCommand { get; }
    public Command ClearDateCommand { get; }
    public bool IsFormEnabled => !_isLoading && !_isSaving && !_isNavigating && !IsSubtaskDialogOpen;
    private bool CanEditDraft => _initialized && !_saved && IsFormEnabled;
    public bool HasSaveError => !string.IsNullOrEmpty(SaveErrorMessage);

    public string SubtaskDialogTitle => _editingSubtask == null ? "Добавить подзадачу" : "Редактирование подзадачи";
    public string SubtaskConfirmText => _editingSubtask == null ? "Добавить" : "Сохранить";

    public string SubtaskName
    {
        get => _subtaskName;
        set
        {
            if (_subtaskName == value) return;
            _subtaskName = value;
            OnPropertyChanged();
            ConfirmSubtaskCommand.ChangeCanExecute();
        }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool HasPlannedDate => PlannedDate.HasValue;

    public DateTime? PlannedDate
    {
        get => _plannedDate;
        set
        {
            if (_plannedDate == value) return;
            _plannedDate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPlannedDate));
        }
    }

    public bool IsSubtaskDialogOpen
    {
        get => _isSubtaskDialogOpen;
        private set
        {
            if (_isSubtaskDialogOpen == value) return;
            _isSubtaskDialogOpen = value;
            OnPropertyChanged();
            RefreshAvailability();
        }
    }

    public string SaveErrorMessage
    {
        get => _saveErrorMessage;
        private set
        {
            _saveErrorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSaveError));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TaskEditorViewModel(CategoryRepository categoryRepos, TaskService taskService,
        SubtaskRepository subtaskRepository, INavigationService navigation)
    {
        _categoryRepos = categoryRepos;
        _taskService = taskService;
        _subtaskRepository = subtaskRepository;
        _navigation = navigation;
        SelectCategoryCommand = new Command<Category>(SelectCategory);
        SaveTaskCommand = new Command(async () => await SaveTaskAsync(),
            () => CanEditDraft);
        BackCommand = new Command(async () => await GoBackAsync(),
            () => !_isLoading && !_isSaving && !_isNavigating && !IsSubtaskDialogOpen);
        AddSubtaskCommand = new Command(() => OpenSubtaskDialog(null), () => CanEditDraft);
        EditSubtaskCommand = new Command<Subtask>(subtask =>
        {
            if (subtask != null && Subtasks.Contains(subtask)) OpenSubtaskDialog(subtask);
        }, subtask => CanEditDraft && subtask != null);
        RemoveSubtaskCommand = new Command<Subtask>(subtask =>
        {
            if (CanEditDraft && subtask != null) Subtasks.Remove(subtask);
        }, subtask => CanEditDraft && subtask != null);
        ConfirmSubtaskCommand = new Command(ConfirmSubtask,
            () => IsSubtaskDialogOpen && !string.IsNullOrWhiteSpace(SubtaskName));
        CancelSubtaskCommand = new Command(CloseSubtaskDialog, () => IsSubtaskDialogOpen);
        ClearDateCommand = new Command(() =>
        {
            if (CanEditDraft) PlannedDate = null;
        }, () => CanEditDraft);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        IsEditMode = query.TryGetValue("mode", out var mode) && mode?.ToString() == "edit";
        _taskId = query.TryGetValue("taskId", out var id) && int.TryParse(id?.ToString(), out var parsed)
            ? parsed : 0;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(SubHeader));
        OnPropertyChanged(nameof(SaveButtonText));
        SaveTaskCommand.ChangeCanExecute();
    }

    public async Task InitializeAsync()
    {

        if (_initialized || _isLoading) return;
        _isLoading = true;
        RefreshAvailability();
        try
        {
            SaveErrorMessage = "";
            await LoadCategoriesAsync();
            if (IsEditMode)
            {
                if (_taskId <= 0) throw new ArgumentException("Не указана задача для редактирования.");
                var task = await _taskService.GetTaskByIdAsync(_taskId)
                    ?? throw new KeyNotFoundException("Задача не найдена. Вернитесь к списку.");
                var subtasks = await _subtaskRepository.GetByTaskIdAsync(_taskId);
                Name = task.Name;
                SelectedCategoryId = task.CategoryId;
                OnPropertyChanged(nameof(SelectedCategoryId));
                PlannedDate = task.DueDate;
                _originalDueDate = task.DueDate?.Date;
                Subtasks.Clear();
                foreach (var subtask in subtasks) Subtasks.Add(subtask);
            }
            _initialized = true;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            SaveErrorMessage = "Не удалось загрузить данные. Вернитесь к списку и откройте редактор снова.";
        }
        finally
        {
            _isLoading = false;
            RefreshAvailability();
        }
    }

    public async Task LoadCategoriesAsync()
    {
        var categories = await _categoryRepos.GetAllAsync();
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);
    }

    private async Task SaveTaskAsync()
    {
        if (!CanEditDraft) return;
        SaveErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Name))
        {
            SaveErrorMessage = "Введите название задачи.";
            return;
        }
        if (PlannedDate.HasValue && PlannedDate.Value.Date < DateTime.Today &&
            (!IsEditMode || PlannedDate.Value.Date != _originalDueDate))
        {
            SaveErrorMessage = "Нельзя установить новую дату выполнения в прошлом.";
            return;
        }

        _isSaving = true;
        RefreshAvailability();
        try
        {
            if (IsEditMode)
            {
                await _taskService.UpdateTaskAsync(new UpdateTaskRequest
                {
                    Id = _taskId,
                    Name = Name.Trim(),
                    CategoryId = SelectedCategoryId,
                    DueDate = PlannedDate?.Date,
                    Subtasks = Subtasks.Select(subtask => new UpdateSubtaskDraft
                    {
                        Id = subtask.Id,
                        Name = subtask.Name,
                        IsCompleted = subtask.IsCompleted
                    }).ToList()
                });
            }
            else
            {
                await _taskService.CreateTaskAsync(new CreateTaskRequest
                {
                    Name = Name.Trim(),
                    CategoryId = SelectedCategoryId > 0 ? SelectedCategoryId : null,
                    DueDate = PlannedDate?.Date,
                    Subtasks = Subtasks.Select(subtask => new SubtaskDraft
                    {
                        Name = subtask.Name,
                        IsCompleted = subtask.IsCompleted
                    }).ToList()
                });
            }
            _saved = true;
            await _navigation.GoBackAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            SaveErrorMessage = _saved
                ? "Задача сохранена. Вернитесь к списку кнопкой назад."
                : "Не удалось сохранить задачу. Проверьте данные и попробуйте ещё раз.";
        }
        finally
        {
            _isSaving = false;
            RefreshAvailability();
        }
    }

    private void SelectCategory(Category? category)
    {
        if (!CanEditDraft || category == null) return;
        SelectedCategoryId = category.Id;
        OnPropertyChanged(nameof(SelectedCategoryId));
    }

    private void OpenSubtaskDialog(Subtask? subtask)
    {
        if (!CanEditDraft) return;
        _editingSubtask = subtask;
        SubtaskName = subtask?.Name ?? "";
        OnPropertyChanged(nameof(SubtaskDialogTitle));
        OnPropertyChanged(nameof(SubtaskConfirmText));
        IsSubtaskDialogOpen = true;
    }

    private void ConfirmSubtask()
    {
        if (!IsSubtaskDialogOpen || string.IsNullOrWhiteSpace(SubtaskName)) return;
        if (_editingSubtask == null)
        {
            Subtasks.Add(new Subtask { Name = SubtaskName.Trim() });
        }
        else if (Subtasks.Contains(_editingSubtask))
        {
            _editingSubtask.Name = SubtaskName.Trim();
        }
        CloseSubtaskDialog();
    }

    private void CloseSubtaskDialog()
    {
        _editingSubtask = null;
        SubtaskName = "";
        IsSubtaskDialogOpen = false;
        OnPropertyChanged(nameof(SubtaskDialogTitle));
        OnPropertyChanged(nameof(SubtaskConfirmText));
    }

    private async Task GoBackAsync()
    {
        if (_isLoading || _isSaving || _isNavigating || IsSubtaskDialogOpen) return;
        _isNavigating = true;
        RefreshAvailability();
        try
        {
            await _navigation.GoBackAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            SaveErrorMessage = "Не удалось вернуться назад. Попробуйте ещё раз.";
        }
        finally
        {
            _isNavigating = false;
            RefreshAvailability();
        }
    }

    private void RefreshAvailability()
    {
        OnPropertyChanged(nameof(IsFormEnabled));
        SaveTaskCommand.ChangeCanExecute();
        BackCommand.ChangeCanExecute();
        AddSubtaskCommand.ChangeCanExecute();
        EditSubtaskCommand.ChangeCanExecute();
        RemoveSubtaskCommand.ChangeCanExecute();
        ConfirmSubtaskCommand.ChangeCanExecute();
        CancelSubtaskCommand.ChangeCanExecute();
        ClearDateCommand.ChangeCanExecute();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
