using System.Windows.Input;

namespace ToDoList.Views.Components;

public partial class ConfirmationDialog : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(ConfirmationDialog),
            "Вы уверены, что хотите удалить?");

    public static readonly BindableProperty ConfirmCommandProperty =
        BindableProperty.Create(
            nameof(ConfirmCommand),
            typeof(ICommand),
            typeof(ConfirmationDialog));

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(
            nameof(CancelCommand),
            typeof(ICommand),
            typeof(ConfirmationDialog));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ConfirmationDialog()
    {
        InitializeComponent();
    }
}