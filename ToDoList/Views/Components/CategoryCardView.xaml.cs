using ToDoList.Models;

namespace ToDoList.Views.Components;

public partial class CategoryCardView : ContentView
{
    private bool _isHovered;

    public static readonly BindableProperty SelectedCategoryIdProperty =
        BindableProperty.Create(
            nameof(SelectedCategoryId),
            typeof(int),
            typeof(CategoryCardView),
            -1,
            propertyChanged: OnSelectedCategoryIdChanged);

    public int SelectedCategoryId
    {
        get => (int)GetValue(SelectedCategoryIdProperty);
        set => SetValue(SelectedCategoryIdProperty, value);
    }

    public CategoryCardView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        UpdateAppearance();
    }

    private static void OnSelectedCategoryIdChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var card = (CategoryCardView)bindable;

        card.UpdateAppearance();
    }

    private void OnPointerEntered(
        object? sender,
        PointerEventArgs e)
    {
        _isHovered = true;

        UpdateAppearance();
    }

    private void OnPointerExited(
        object? sender,
        PointerEventArgs e)
    {
        _isHovered = false;

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (CategoryBorder == null ||
            BindingContext is not Category category)
        {
            return;
        }

        bool isSelected = category.Id == SelectedCategoryId;

        string alpha;

        if (isSelected)
        {
            alpha = _isHovered ? "77" : "66";
        }
        else
        {
            alpha = _isHovered ? "44" : "33";
        }

        CategoryBorder.BackgroundColor =
            Color.FromArgb("#" + alpha + category.Color.TrimStart('#'));

        CategoryBorder.StrokeThickness = isSelected ? 2.5 : 1.5;
    }
}