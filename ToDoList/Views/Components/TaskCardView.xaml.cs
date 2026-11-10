namespace ToDoList.Views.Components;

public partial class TaskCardView : ContentView
{
    private bool _isUpdatingCompletion;

	public TaskCardView()
	{
		InitializeComponent();
	}

    private async void OnCompletionCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingCompletion ||
            BindingContext is not ToDoList.Models.TaskItem task ||
            task.IsCompleted == e.Value)
        {
            return;
        }

        Element? parent = Parent;
        while (parent != null && parent is not ContentPage)
            parent = parent.Parent;

        if (parent is not ContentPage page ||
            page.BindingContext is not ToDoList.ViewModels.MainViewModel viewModel)
        {
            CompletionCheckBox.IsChecked = task.IsCompleted;
            return;
        }

        _isUpdatingCompletion = true;
        CompletionCheckBox.IsEnabled = false;
        try
        {
            await viewModel.SetTaskCompletedAsync(task, e.Value);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            await page.DisplayAlertAsync("Ошибка", "Не удалось сохранить отметку выполнения. Попробуйте ещё раз.", "ОК");
        }
        finally
        {
            // Карточка могла получить другую задачу, пока выполнялось сохранение.
            if (BindingContext is ToDoList.Models.TaskItem currentTask)
                CompletionCheckBox.IsChecked = currentTask.IsCompleted;

            CompletionCheckBox.IsEnabled = true;
            _isUpdatingCompletion = false;
        }
    }

    private async void OnCreateTaskClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ToDoList.Models.TaskItem task)
        {
            await Shell.Current.GoToAsync($"task-editor?mode=edit&taskId={task.Id}");
        }
    }

}
