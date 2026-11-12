using ToDoList.ViewModels;

namespace ToDoList.Views;

public partial class TaskDetailsPage : ContentPage
{
    private readonly TaskDetailsViewModel _viewModel;

    public TaskDetailsPage(TaskDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
