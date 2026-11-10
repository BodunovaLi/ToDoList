using ToDoList.ViewModels;

namespace ToDoList.Views;

public partial class TaskEditorPage : ContentPage
{
    private readonly TaskEditorViewModel _viewModel;
    private ToDoList.Models.Subtask? _editingSubtask;
    public TaskEditorPage(TaskEditorViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void GoBack(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void AddSubtask(object? sender, EventArgs e)
    {
        _editingSubtask = null;
        SubtaskDialog.Prepare();
        _viewModel.IsSubtaskDialogOpen = true;
        SubtaskDialog.IsVisible = true;
    }

    private void OnSubtaskCancelRequested(object? sender, EventArgs e)
    {
        _editingSubtask = null;
        SubtaskDialog.IsVisible = false;
        _viewModel.IsSubtaskDialogOpen = false;
    }

    private void OnSelectDateClicked(object? sender, EventArgs e)
    {
        DueDatePicker.IsOpen = true;
    }

    private void OnClearDateClicked(object? sender, EventArgs e)
    {
        _viewModel.PlannedDate = null;
    }

    private void OnSubtaskAddRequested(object? sender, string name)
    {
        if (_editingSubtask == null)
            _viewModel.AddSubtask(name);
        else
            _viewModel.EditSubtask(_editingSubtask, name);

        _editingSubtask = null;

        SubtaskDialog.IsVisible = false;
        _viewModel.IsSubtaskDialogOpen = false;
    }

    private void OnSubtaskEditRequested(object? sender, ToDoList.Models.Subtask subtask)
    {
        _editingSubtask = subtask;
        SubtaskDialog.Prepare(subtask.Name);
        _viewModel.IsSubtaskDialogOpen = true;
        SubtaskDialog.IsVisible = true;
    }

    private void OnSubtaskDeleteRequested(object? sender, ToDoList.Models.Subtask subtask)
    {
        _viewModel.RemoveSubtask(subtask);
    }

}
