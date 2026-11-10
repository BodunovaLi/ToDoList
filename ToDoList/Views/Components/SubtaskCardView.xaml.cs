namespace ToDoList.Views.Components;

public partial class SubtaskCardView : ContentView
{
    public event EventHandler<ToDoList.Models.Subtask>? EditRequested;
    public event EventHandler<ToDoList.Models.Subtask>? DeleteRequested;

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ToDoList.Models.Subtask subtask)
            EditRequested?.Invoke(this, subtask);
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ToDoList.Models.Subtask subtask)
            DeleteRequested?.Invoke(this, subtask);
    }
	public SubtaskCardView()
	{
		InitializeComponent();
	}
}
