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