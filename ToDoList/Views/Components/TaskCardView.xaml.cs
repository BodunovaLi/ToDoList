using System.Windows.Input;

namespace ToDoList.Views.Components;

public partial class TaskCardView : ContentView
{
    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(TaskCardView));
    public static readonly BindableProperty EditCommandProperty = BindableProperty.Create(
        nameof(EditCommand), typeof(ICommand), typeof(TaskCardView));
    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand), typeof(ICommand), typeof(TaskCardView));
    public static readonly BindableProperty ToggleCompletionCommandProperty = BindableProperty.Create(
        nameof(ToggleCompletionCommand), typeof(ICommand), typeof(TaskCardView));

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand? ToggleCompletionCommand
    {
        get => (ICommand?)GetValue(ToggleCompletionCommandProperty);
        set => SetValue(ToggleCompletionCommandProperty, value);
    }

    public TaskCardView()
    {
        InitializeComponent();
    }
}
