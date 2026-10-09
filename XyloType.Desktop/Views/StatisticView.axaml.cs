using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.Statistic;

namespace XyloType.Desktop.Views;

/// <summary>
/// The results of an exercise. The next exercise is focused (the same one again at the end of the list):
/// Enter chains; a space typed after the last letter does not press it.
/// </summary>
public partial class StatisticView : UserControl, IViewLifecycle
{
    // keys still typed at the end of the exercise must not press the focused button
    private static readonly TimeSpan s_focusDelay = TimeSpan.FromMilliseconds(400);

    public StatisticView(StatisticChartsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // only Enter chains: a space would press the button too
        foreach (Button button in new[] { NextButton, RetryButton })
            button.AddHandler(KeyDownEvent, IgnoreSpace, RoutingStrategies.Tunnel);
    }

    private static void IgnoreSpace(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            e.Handled = true;
    }

    public async void OnAppearing()
    {
        await Task.Delay(s_focusDelay);
        await Dispatcher.UIThread.InvokeAsync(() => (NextButton.IsEffectivelyEnabled ? NextButton : RetryButton).Focus());
    }

    public void OnDisappearing()
    {
    }
}
