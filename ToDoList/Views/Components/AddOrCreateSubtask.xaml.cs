namespace ToDoList.Views.Components;

public partial class AddOrCreateSubtask : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AddOrCreateSubtask), "", BindingMode.TwoWay);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(AddOrCreateSubtask), "Добавить подзадачу");
    public static readonly BindableProperty ConfirmationTextProperty = BindableProperty.Create(
        nameof(ConfirmationText), typeof(string), typeof(AddOrCreateSubtask), "Добавить");
    public static readonly BindableProperty ConfirmCommandProperty = BindableProperty.Create(
        nameof(ConfirmCommand), typeof(System.Windows.Input.ICommand), typeof(AddOrCreateSubtask));
    public static readonly BindableProperty CancelCommandProperty = BindableProperty.Create(
        nameof(CancelCommand), typeof(System.Windows.Input.ICommand), typeof(AddOrCreateSubtask));
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
        nameof(ErrorText), typeof(string), typeof(AddOrCreateSubtask), "",
        propertyChanged: (bindable, _, _) => ((AddOrCreateSubtask)bindable).OnPropertyChanged(nameof(HasError)));

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }
    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string ConfirmationText
    {
        get => (string)GetValue(ConfirmationTextProperty);
        set => SetValue(ConfirmationTextProperty, value);
    }
    public System.Windows.Input.ICommand? ConfirmCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }
    public System.Windows.Input.ICommand? CancelCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public AddOrCreateSubtask()
	{
		InitializeComponent();
	}

}
