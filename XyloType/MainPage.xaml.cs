using XyloType.Application.Models.Themes;
using XyloType.MVVM.Controls;
using XyloType.Navigation;
using XyloType.ViewModels.Theme;

namespace XyloType;

/// <summary>
/// The single page of the app: the views are shown in its host by the <see cref="AppNavigator"/>.
/// </summary>
public partial class MainPage : ContentPage
{
    private const double TooltipGap = 6;

    private readonly ThemeViewModel _theme;
    private readonly AccentColorViewModel _accent;

    public MainPage(AppNavigator navigator, ThemeViewModel theme, AccentColorViewModel accent)
    {
        InitializeComponent();

        _theme = theme;
        _accent = accent;
        ThemeGroup.BindingContext = theme;
        theme.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeViewModel.Theme))
                MoveThemePill(animated: true);
        };
        MoveThemePill(animated: false);
        AccentPicker.BindingContext = accent;
        accent.PropertyChanged += (_, _) => PlacePickerMarkers();
        ShadeSquare.SizeChanged += (_, _) => PlacePickerMarkers();
        HueBar.SizeChanged += (_, _) => PlacePickerMarkers();

        navigator.CurrentViewChanged += view => ShowView(navigator, view);
        navigator.StateChanged += () => Rail.Update(navigator.CurrentSection, navigator.ExerciseView is not null, navigator.ExerciseView is MVVM.Views.StatisticView);
        navigator.BusyChanged += ShowBusy;
        Rail.SectionClicked += async section => await navigator.GoToSectionAsync(section);
        Rail.TooltipRequested += (text, button) => ShowTooltip(text, button, onTheRight: true);

        AddTooltip(LightThemeButton, "Thème clair");
        AddTooltip(DarkThemeButton, "Thème sombre");
        AddTooltip(SystemThemeButton, "Thème du système");
        AddTooltip(AccentButton, "Couleur principale");

        navigator.Start();
    }

    private void ShowView(AppNavigator navigator, View view)
    {
        if (!ViewHost.Children.Contains(view))
            ViewHost.Children.Add(view);

        foreach (View child in ViewHost.Children.OfType<View>().ToList())
        {
            if (child == view)
                child.IsVisible = true;
            else if (navigator.IsKept(child))
                child.IsVisible = false;
            else
                ViewHost.Children.Remove(child);
        }
    }

    #region Loading veil

    private void ShowBusy(bool busy)
    {
        BusyVeil.IsVisible = busy;
        BusySpinner.IsRunning = busy;
    }

    #endregion

    #region Theme (right rail)

    private const string ThemePillAnimation = "ThemePill";

    /// <summary>
    /// The pill goes under the button of the theme chosen, sliding like the selection of the settings sections.
    /// </summary>
    private void MoveThemePill(bool animated)
    {
        int index = _theme.Theme switch
        {
            ThemeStateConfiguration.Light => 0,
            ThemeStateConfiguration.Dark => 1,
            _ => 2,
        };

        // from the place of the button in the column: known before the buttons are laid out (at start)
        double target = index * (ThemePill.HeightRequest + ThemeButtons.Spacing);
        ThemePill.AbortAnimation(ThemePillAnimation);

        if (!animated)
        {
            ThemePill.TranslationY = target;
            return;
        }

        double from = ThemePill.TranslationY;
        new Animation(progress => ThemePill.TranslationY = from + (target - from) * progress)
            .Commit(ThemePill, ThemePillAnimation, length: 220, easing: Easing.CubicOut);
    }


    private void LightTheme_Clicked(object? sender, EventArgs e) => _theme.SetTheme(ThemeStateConfiguration.Light);
    private void DarkTheme_Clicked(object? sender, EventArgs e) => _theme.SetTheme(ThemeStateConfiguration.Dark);
    private void SystemTheme_Clicked(object? sender, EventArgs e) => _theme.SetTheme(ThemeStateConfiguration.System);

    #endregion

    #region Main color picker

    private bool _isPicking;

    private void AccentButton_Clicked(object? sender, EventArgs e)
    {
        AccentPickerLayer.IsVisible = !AccentPickerLayer.IsVisible;
        PlacePickerMarkers();
    }

    // a click outside the picker closes it
    private void AccentPickerLayer_Tapped(object? sender, TappedEventArgs e)
    {
        if (e.GetPosition(AccentPicker) is Point p && p.X >= 0 && p.Y >= 0 && p.X <= AccentPicker.Width && p.Y <= AccentPicker.Height)
            return;

        AccentPickerLayer.IsVisible = false;
    }

    private void ShadeSquare_PointerPressed(object? sender, PointerEventArgs e)
    {
        _isPicking = true;
        PickShade(e);
    }

    private void ShadeSquare_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPicking)
            PickShade(e);
    }

    private void HueBar_PointerPressed(object? sender, PointerEventArgs e)
    {
        _isPicking = true;
        PickHue(e);
    }

    private void HueBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPicking)
            PickHue(e);
    }

    private void Picker_PointerReleased(object? sender, PointerEventArgs e)
        => _isPicking = false;

    private void PickShade(PointerEventArgs e)
    {
        if (e.GetPosition(ShadeSquare) is not Point p || ShadeSquare.Width <= 0)
            return;

        _accent.PickShade(p.X / ShadeSquare.Width, 1 - p.Y / ShadeSquare.Height);
    }

    private void PickHue(PointerEventArgs e)
    {
        if (e.GetPosition(HueBar) is not Point p || HueBar.Width <= 0)
            return;

        _accent.PickHue(p.X / HueBar.Width * 360);
    }

    /// <summary>
    /// Markers on the shade square and the hue bar, where the current color is.
    /// </summary>
    private void PlacePickerMarkers()
    {
        if (ShadeSquare.Width <= 0 || HueBar.Width <= 0)
            return;

        ShadeMarker.TranslationX = _accent.Saturation * ShadeSquare.Width - ShadeMarker.WidthRequest / 2;
        ShadeMarker.TranslationY = (1 - _accent.Value) * ShadeSquare.Height - ShadeMarker.HeightRequest / 2;
        HueMarker.TranslationX = _accent.Hue / 360 * HueBar.Width - HueMarker.WidthRequest / 2;
    }

    #endregion

    #region Tooltips of the rails

    private void AddTooltip(NavRailButton button, string text)
    {
        button.PointerEntered += (_, _) => ShowTooltip(text, button, onTheRight: false);
        button.PointerExited += (_, _) => ShowTooltip(null, button, onTheRight: false);
    }

    /// <summary>
    /// Shows the tooltip beside the button, level with it: on the right of the left rail, on the left of the right rail.
    /// </summary>
    private void ShowTooltip(string? text, View button, bool onTheRight)
    {
        if (text is null)
        {
            RailTooltip.IsVisible = false;
            return;
        }

        // position of the button in the page
        double y = button.Y;
        for (Element? parent = button.Parent; parent is VisualElement element && parent is not ContentPage; parent = parent.Parent)
            y += element.Y;

        RailTooltipText.Text = text;
        RailTooltip.HorizontalOptions = onTheRight ? LayoutOptions.Start : LayoutOptions.End;
        RailTooltip.TranslationX = onTheRight
            ? NavRail.RailWidth + TooltipGap
            : -(NavRail.RailWidth + TooltipGap);
        RailTooltip.TranslationY = y + (button.Height - RailTooltip.HeightRequest) / 2;
        RailTooltip.IsVisible = true;
    }

    #endregion
}
