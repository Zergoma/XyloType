using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;

namespace XyloType.ViewModels.Theme;

/// <summary>
/// Theme of the app: light, dark, or following the Windows setting. The user choice is applied at start.
/// </summary>
public partial class ThemeViewModel : ObservableObject
{
    private readonly IThemeChangerService _themeChangerService;

    public ThemeViewModel(IThemeChangerService themeChangerService)
    {
        _themeChangerService = themeChangerService;
        Theme = _themeChangerService.ApplyUserSelectedTheme();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsThemeLight))]
    [NotifyPropertyChangedFor(nameof(IsThemeDark))]
    [NotifyPropertyChangedFor(nameof(IsThemeSystem))]
    public partial ThemeStateConfiguration Theme { get; set; }

    public bool IsThemeLight => Theme == ThemeStateConfiguration.Light;
    public bool IsThemeDark => Theme == ThemeStateConfiguration.Dark;
    public bool IsThemeSystem => Theme == ThemeStateConfiguration.System;

    [RelayCommand]
    public void SetTheme(ThemeStateConfiguration theme)
    {
        switch (theme)
        {
            case ThemeStateConfiguration.Dark: _themeChangerService.SetDark(); break;
            case ThemeStateConfiguration.Light: _themeChangerService.SetLight(); break;
            case ThemeStateConfiguration.System: _themeChangerService.SetToSystem(); break;
            default: throw new NotImplementedException();
        }

        Theme = theme;
    }
}
