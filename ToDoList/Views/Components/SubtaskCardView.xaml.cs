namespace ToDoList.Views.Components;

public partial class SubtaskCardView : ContentView
{
    public static readonly BindableProperty EditCommandProperty = BindableProperty.Create(
        nameof(EditCommand), typeof(System.Windows.Input.ICommand), typeof(SubtaskCardView));
    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand), typeof(System.Windows.Input.ICommand), typeof(SubtaskCardView));

    public System.Windows.Input.ICommand? EditCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public System.Windows.Input.ICommand? DeleteCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
	public SubtaskCardView()
	{
		InitializeComponent();
	}
}
