using System.Windows.Input;

namespace ToDoList.Views.Components;


public sealed class CommandCheckBox : ContentView
{
    private bool _synchronizing;
    private readonly CheckBox _checkBox;

    public static readonly BindableProperty CheckedValueProperty = BindableProperty.Create(
        nameof(CheckedValue), typeof(bool), typeof(CommandCheckBox), false, BindingMode.OneWay,
        propertyChanged: (bindable, _, _) => ((CommandCheckBox)bindable).Synchronize());
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(CommandCheckBox));
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(CommandCheckBox));
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(CommandCheckBox), Colors.Purple,
        propertyChanged: (bindable, _, value) => ((CommandCheckBox)bindable)._checkBox.Color = (Color)value);

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool CheckedValue
    {
        get => (bool)GetValue(CheckedValueProperty);
        set => SetValue(CheckedValueProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public CommandCheckBox()
    {
        _checkBox = new CheckBox { Color = Color };
        _checkBox.CheckedChanged += OnCheckedChanged;
        Content = _checkBox;
    }

    private void OnCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_synchronizing || e.Value == CheckedValue) return;
        try
        {
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        }
        finally
        {

            Synchronize();
        }
    }

    private void Synchronize()
    {
        _synchronizing = true;
        try { _checkBox.IsChecked = CheckedValue; }
        finally { _synchronizing = false; }
    }
}
