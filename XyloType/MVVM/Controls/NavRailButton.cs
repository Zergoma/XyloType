using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace XyloType.MVVM.Controls;

/// <summary>
/// A button of the navigation rail (left edge of the window): a flat icon, shaded on hover,
/// on the accent color for the page being shown.
/// </summary>
public class NavRailButton : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(NavRailButton),
        propertyChanged: (bindable, _, value) => ((NavRailButton)bindable)._path.Data = (Geometry?)value);

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(NavRailButton), false,
        propertyChanged: (bindable, _, _) => ((NavRailButton)bindable).ApplyColors());

    public event EventHandler? Clicked;
    public event EventHandler? PointerEntered;
    public event EventHandler? PointerExited;

    private const double Size = 40;
    private const double IconSize = 20;

    private readonly Path _path;
    private readonly Border _frame;
    private bool _isPointerOver;

    public NavRailButton()
    {
        _path = new Path
        {
            Aspect = Stretch.Uniform,
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        _frame = new Border
        {
            WidthRequest = Size,
            HeightRequest = Size,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = _path,
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        _frame.GestureRecognizers.Add(tap);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) => { _isPointerOver = true; ApplyColors(); PointerEntered?.Invoke(this, EventArgs.Empty); };
        pointer.PointerExited += (_, _) => { _isPointerOver = false; ApplyColors(); PointerExited?.Invoke(this, EventArgs.Empty); };
        _frame.GestureRecognizers.Add(pointer);

        Content = _frame;
        ApplyColors();
    }

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private void ApplyColors()
    {
        if (IsActive)
        {
            _frame.SetAppThemeColor(Border.BackgroundColorProperty, Resource("Primary"), Resource("PrimaryDark"));
            _path.Fill = Colors.White;
            return;
        }

        if (_isPointerOver)
            _frame.SetAppThemeColor(Border.BackgroundColorProperty, Resource("RowHoverBgLight"), Resource("RowHoverBgDark"));
        else
            _frame.BackgroundColor = Colors.Transparent;

        _path.SetAppTheme<Brush>(Shape.FillProperty,
            new SolidColorBrush(Resource(_isPointerOver ? "TextTitleLight" : "TextMutedLight")),
            new SolidColorBrush(Resource(_isPointerOver ? "TextTitleDark" : "TextMutedDark")));
    }

    private static Color Resource(string key)
        => Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.Gray;
}
