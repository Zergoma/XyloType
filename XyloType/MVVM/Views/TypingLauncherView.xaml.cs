using XyloType.Navigation;
using XyloType.ViewModels.TypingLauncher;

using XyloType.Application;
using XyloType.Application.Interfaces;

namespace XyloType.MVVM.Views;

public partial class TypingLauncherView : ContentView, IViewLifecycle
{
	private readonly IUserKeyboardLayoutPreferenceService _userKeyboardPreferenceService;

    public TypingLauncherView(
        TypingLauncherViewModel vm,
        IUserKeyboardLayoutPreferenceService userKeyboardPreferenceService)
    {
        InitializeComponent();
        _userKeyboardPreferenceService = userKeyboardPreferenceService;
        BindingContext = vm;

        // Update user preference
        vm.KeyboardLayoutChanged += OnKeyboardChanged;
    }

    private async Task OnKeyboardChanged(int keyboardId)
    {
        _userKeyboardPreferenceService.SetKeyboardType(keyboardId);

        if (BindingContext is TypingLauncherViewModel vm)
        {
            await vm.InitilizationAsync(keyboardId);
        }
    }

    public async void OnAppearing()
    {
        StartLaunchAnimation();

        if(BindingContext is TypingLauncherViewModel vm)
		{
            Result<int> keyboardCodeResult = _userKeyboardPreferenceService.GetKeyboardType();
            if(!keyboardCodeResult.Success)
            {
                return;
            }

            await vm.InitilizationAsync(keyboardCodeResult.GetValue);
		}
    }

    public void OnDisappearing()
    {
        _launchAnimation?.Cancel();
        _launchAnimation = null;
    }

    #region Launch button

    private CancellationTokenSource? _launchAnimation;

    /// <summary>
    /// Draws the eye to the launch button, every 3.5 seconds: a soft halo spreads out from it
    /// while the button beats once.
    /// </summary>
    private void StartLaunchAnimation()
    {
        _launchAnimation?.Cancel();
        _launchAnimation = new CancellationTokenSource();
        _ = LoopLaunchAnimationAsync(_launchAnimation.Token);
    }

    private async Task LoopLaunchAnimationAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(2300, token);

                await Task.WhenAll(SpreadHaloAsync(), BeatAsync());
            }
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            LaunchHalo.Opacity = 0;
            LaunchButton.Scale = 1;
        }
    }

    /// <summary>
    /// The halo grows a little more in height than in width, to stay inside the card.
    /// </summary>
    private Task SpreadHaloAsync()
    {
        TaskCompletionSource done = new();
        Animation spread = new(progress =>
        {
            LaunchHalo.ScaleX = 1 + 0.12 * progress;
            LaunchHalo.ScaleY = 1 + 0.45 * progress;
            LaunchHalo.Opacity = 0.5 * (1 - progress);
        });

        spread.Commit(LaunchHalo, "Spread", length: 1200, easing: Easing.CubicOut, finished: (_, _) => done.TrySetResult());
        return done.Task;
    }

    private async Task BeatAsync()
    {
        await LaunchButton.ScaleToAsync(1.06, 250, Easing.CubicOut);
        await LaunchButton.ScaleToAsync(1, 350, Easing.CubicIn);
    }

    private void Launch_PointerEntered(object? sender, PointerEventArgs e)
        => LaunchHover.Opacity = 0.12;

    private void Launch_PointerExited(object? sender, PointerEventArgs e)
        => LaunchHover.Opacity = 0;

    #endregion
}
