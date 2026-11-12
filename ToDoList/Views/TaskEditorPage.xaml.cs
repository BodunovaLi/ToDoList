using ToDoList.ViewModels;

namespace ToDoList.Views;

public partial class TaskEditorPage : ContentPage
{
    private readonly TaskEditorViewModel _viewModel;

    public TaskEditorPage(TaskEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private void OnSelectDateClicked(object? sender, EventArgs e)
    {
        DueDatePicker.IsOpen = true;
    }
}
