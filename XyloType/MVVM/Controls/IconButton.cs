using Microsoft.Maui.Controls.Shapes;

using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace XyloType.MVVM.Controls;

/// <summary>
/// A small round button showing a vector icon (see <see cref="Icons"/>), with a hover color.
/// </summary>
public class IconButton : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(Geometry), typeof(IconButton),
        propertyChanged: (bindable, _, value) => ((IconButton)bindable)._path.Data = (Geometry?)value);

    /// <summary>
    /// Toggle buttons: highlighted with the accent color when on.
    /// </summary>
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(IconButton), false,
        propertyChanged: (bindable, _, _) => ((IconButton)bindable).ApplyBackground());

    public event EventHandler? Clicked;

    private const double Size = 32;
    private const double IconSize = 14;

    private readonly Path _path;
    private readonly Border _circle;
    private bool _isPointerOver;

    public IconButton()
    {
        _path = new Path
        {
            Aspect = Stretch.Uniform,
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        _circle = new Border
        {
            WidthRequest = Size,
            HeightRequest = Size,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = Size / 2 },
            Content = _path,
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) =>
        {
            if (IsEnabled)
                Clicked?.Invoke(this, EventArgs.Empty);
        };
        _circle.GestureRecognizers.Add(tap);

        PointerGestureRecognizer pointer = new();
        pointer.PointerEntered += (_, _) => { _isPointerOver = true; ApplyBackground(); };
        pointer.PointerExited += (_, _) => { _isPointerOver = false; ApplyBackground(); };
        _circle.GestureRecognizers.Add(pointer);

        Content = _circle;

        ApplyIconStyle();
        ApplyBackground();
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

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // disabled: faded and no hover
        if (propertyName == nameof(IsEnabled))
        {
            Opacity = IsEnabled ? 1 : 0.35;
            ApplyBackground();
        }
    }

    private void ApplyIconStyle()
    {
        _path.Fill = Colors.White;
        _path.StrokeThickness = 0;
    }

    /// <summary>
    /// Same colors as the neutral buttons (button_hover style), or the primary ones when active, following the theme.
    /// </summary>
    private void ApplyBackground()
    {
        string kind = IsActive ? "Primary" : "Neutral";
        string state = _isPointerOver && IsEnabled ? "Hover" : string.Empty;

        // theme bound color: follows a light / dark switch by itself
        _circle.SetAppThemeColor(
            Border.BackgroundColorProperty,
            Resource($"Button{kind}Bg{state}Light"),
            Resource($"Button{kind}Bg{state}Dark"));
    }

    private static Color Resource(string key)
        => Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.Gray;
}
