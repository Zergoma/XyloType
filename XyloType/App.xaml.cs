using MauiAppNS = Microsoft.Maui.Controls;

namespace XyloType;

public partial class App : MauiAppNS.Application
{
    private readonly IServiceProvider _serviceProvider;
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // the main color chosen by the user, before the page is drawn
        _serviceProvider.GetRequiredService<Services.MauiAccentColorService>().Apply();

        // the audio device opens in the background: the first exercise does not wait for it
        _ = _serviceProvider.GetRequiredService<Application.Interfaces.IPlaySoundSample>().PreloadAsync();

        // the colors of the letters are read once, at start
        _ = Task.Run(() => _serviceProvider.GetRequiredService<Application.Interfaces.Typing.ITypingThemeProvider>()
            .GetThemeAsync(ViewModels.Typing.TypingViewModel.TypingThemeName, Application.Models.Themes.ThemeState.Light));

        Window win = new Window(_serviceProvider.GetRequiredService<MainPage>());
#if WINDOWS
        win.Height = 800;
        win.Width = 1000;

        win.MinimumHeight = 800;
        win.MinimumWidth = 900;
#endif
        return win;
    }
}