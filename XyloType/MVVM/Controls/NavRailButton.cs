using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace XyloType.MVVM.Controls;

/// <summary>
/// A button of the navigation rail (left edge of the window): a flat icon, shaded on hover,
/// on the main color for the page being shown.
/// The main color is a dynamic resource set once on its own layer, only shown or hidden:
/// removing and setting it again did not apply it any more.
/// </summary>
public class NavRailButton : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(NavRailButton),
        propertyChanged: (bindable, _, value) =>
        {
            NavRailButton button = (NavRailButton)bindable;
            button._path.Data = (Geometry?)value;
            button._activePath.Data = (Geometry?)value;
        });

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(NavRailButton), false,
        propertyChanged: (bindable, _, _) => ((NavRailButton)bindable).ApplyColors());

    /// <summary>
    /// False when a group draws the selection itself (a sliding pill): the active button only changes its icon color.
    /// </summary>
    public static readonly BindableProperty UsesActiveBackgroundProperty = BindableProperty.Create(
        nameof(UsesActiveBackground), typeof(bool), typeof(NavRailButton), true,
        propertyChanged: (bindable, _, _) => ((NavRailButton)bindable).ApplyColors());

    public event EventHandler? Clicked;
    public event EventHandler? PointerEntered;
    public event EventHandler? PointerExited;

    private const double Size = 40;
    private const double IconSize = 20;

    // icon of an inactive button (muted, brighter on hover) and of the active one (on the main color)
    private readonly Path _path;
    private readonly Path _activePath;
    private readonly BoxView _activeLayer;
    private readonly Border _frame;
    private bool _isPointerOver;

    public NavRailButton()
    {
        _path = NewIcon();
        _activePath = NewIcon();
        _activePath.SetDynamicResource(Shape.FillProperty, "AccentForeground");

        _activeLayer = new BoxView();
        _activeLayer.SetDynamicResource(BoxView.ColorProperty, "Accent");

        _frame = new Border
        {
            WidthRequest = Size,
            HeightRequest = Size,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Grid { Children = { _activeLayer, _path, _activePath } },
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

    private static Path NewIcon()
        => new()
        {
            Aspect = Stretch.Uniform,
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

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

    public bool UsesActiveBackground
    {
        get => (bool)GetValue(UsesActiveBackgroundProperty);
        set => SetValue(UsesActiveBackgroundProperty, value);
    }

    private void ApplyColors()
    {
        _activeLayer.IsVisible = IsActive && UsesActiveBackground;
        _activePath.IsVisible = IsActive;
        _path.IsVisible = !IsActive;

        if (_isPointerOver && !IsActive)
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
