using ToDoList.ViewModels;

namespace ToDoList;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadCategoriesAsync();
        await _viewModel.LoadTasksAsync();
    }

    private async void OnCreateTaskClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("task-editor?mode=create");
    }

}