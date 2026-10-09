using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models;
using XyloType.Application.Models.Themes;
using XyloType.Desktop.Composition;
using XyloType.Desktop.Services;
using XyloType.Desktop.Views;
using XyloType.Domain.Enums;
using XyloType.Infrastructure.DI;
using XyloType.ViewModels.Typing;

namespace XyloType.Desktop;

public partial class App : global::Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = DesktopServices.Build();

            // the database is created or upgraded before anything reads it
            _services.InitUpgradeInfrastructure();

            // the colors of the user, before the window is drawn
            _services.GetRequiredService<AvaloniaAccentColorService>().Apply();
            _services.GetRequiredService<IThemeChangerService>().ApplyUserSelectedTheme();

            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) => _services.Dispose();

            WarmUp(_services);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// What makes the first exercise slow is done in the background at start, as in the MAUI app:
    /// the audio device, the typing colors, the first query of real words.
    /// </summary>
    private static void WarmUp(IServiceProvider services)
    {
        _ = services.GetRequiredService<IPlaySoundSample>().PreloadAsync();

        _ = Task.Run(() => services.GetRequiredService<ITypingThemeProvider>()
            .GetThemeAsync(TypingViewModel.TypingThemeName, ThemeState.Light));

        _ = Task.Run(() => services.GetRequiredService<IImportedWordsGenerator>()
            .GenerateAsync(new ImportedWordsOptions(["fr"], "a", 1, 1, KeyboardLayout.AzertyFr), 1));
    }
}
