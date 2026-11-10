namespace ToDoList.Views.Components;

public partial class AddOrCreateSubtask : ContentView
{
    public event EventHandler? CancelRequested;
    public event EventHandler<string>? AddRequested;
    public void Prepare(string? name = null)
    {
        SubtaskNameEntry.Text = name ?? "";
        DialogTitle.Text = name == null ? "Добавить подзадачу" : "Редактирование подзадачи";
        ConfirmButton.Text = name == null ? "Добавить" : "Сохранить";
    }
    public AddOrCreateSubtask()
	{
		InitializeComponent();
	}

    private void OnAddClicked(object? sender, EventArgs e)
    {
        string name = SubtaskNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        AddRequested?.Invoke(this, name);

        SubtaskNameEntry.Text = "";
    }

    private void GoBack(object? sender, EventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }




}
