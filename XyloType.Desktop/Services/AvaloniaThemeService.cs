using Avalonia.Styling;

using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;

namespace XyloType.Desktop.Services;

/// <summary>
/// The theme of the app (light, dark or the one of the system), remembered in the settings.
/// </summary>
public class AvaloniaThemeService : IThemeChangerService
{
    private const string ThemeKey = "Theme";

    private readonly ISettingsStore _store;

    public AvaloniaThemeService(ISettingsStore store)
    {
        _store = store;
    }

    public void SetDark() => Apply(ThemeStateConfiguration.Dark);

    public void SetLight() => Apply(ThemeStateConfiguration.Light);

    public void SetToSystem() => Apply(ThemeStateConfiguration.System);

    public ThemeStateConfiguration ApplyUserSelectedTheme()
    {
        ThemeStateConfiguration theme = _store.Get(ThemeKey, "System") switch
        {
            "Light" => ThemeStateConfiguration.Light,
            "Dark" => ThemeStateConfiguration.Dark,
            _ => ThemeStateConfiguration.System,
        };

        SetVariant(theme);
        return theme;
    }

    public ThemeState GetTheme()
        => global::Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Light ? ThemeState.Light : ThemeState.Dark;

    private void Apply(ThemeStateConfiguration theme)
    {
        _store.Set(ThemeKey, theme.ToString());
        SetVariant(theme);
    }

    private static void SetVariant(ThemeStateConfiguration theme)
    {
        if (global::Avalonia.Application.Current is not { } app)
            return;

        app.RequestedThemeVariant = theme switch
        {
            ThemeStateConfiguration.Light => ThemeVariant.Light,
            ThemeStateConfiguration.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
