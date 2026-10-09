using XyloType.MVVM.ViewModels;
using XyloType.Navigation;

namespace XyloType.MVVM.Views;

public partial class StatisticView : ContentView, IViewLifecycle
{
    // keys still typed at the end of the exercise must not press the focused button
    private static readonly TimeSpan s_focusDelay = TimeSpan.FromMilliseconds(400);

    // space around the floating header and actions bar: the results show through it when scrolling
    public static readonly Thickness BarMargin = new(16, 12);

    // gap between a floating bar and the first / last result card
    private const double ContentGap = 16;

    public StatisticView(StatisticViewModelMauiAdapter vm)
    {
        InitializeComponent();
        BindingContext = vm;

        HeaderBar.SizeChanged += (_, _) => UpdateContentPadding();
        ActionsBar.SizeChanged += (_, _) => UpdateContentPadding();

#if WINDOWS
        // only Enter chains: a space typed after the last letter would press the button
        foreach (Button button in new[] { NextButton, RetryButton })
        {
            button.HandlerChanged += (_, _) =>
            {
                if (button.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement native)
                {
                    native.PreviewKeyDown += IgnoreSpace;
                    native.PreviewKeyUp += IgnoreSpace;
                }
            };
        }
#endif
    }

#if WINDOWS
    private static void IgnoreSpace(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Space)
            e.Handled = true;
    }
#endif

    /// <summary>
    /// The next exercise is focused (the same one again at the end of the list): Enter chains.
    /// </summary>
    public async void OnAppearing()
    {
        await Task.Delay(s_focusDelay);
        (NextButton.IsEnabled ? NextButton : RetryButton).Focus();
    }

    public void OnDisappearing()
    {
    }

    // the grouping labels switch their grouping too
    private void GroupResponseTimes_Tapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is StatisticViewModelMauiAdapter vm)
            vm.Core.GroupResponseTimes = !vm.Core.GroupResponseTimes;
    }

    private void GroupErrors_Tapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is StatisticViewModelMauiAdapter vm)
            vm.Core.GroupErrors = !vm.Core.GroupErrors;
    }

    /// <summary>
    /// Keeps the first and last results reachable: they must be able to scroll out from under the bars.
    /// </summary>
    private void UpdateContentPadding()
    {
        Thickness padding = ResultsContent.Padding;

        double top = HeaderBar.Height + BarMargin.Top + BarMargin.Bottom + ContentGap;
        double bottom = ActionsBar.Height + BarMargin.Top + BarMargin.Bottom + ContentGap;

        if (HeaderBar.Height <= 0 || ActionsBar.Height <= 0)
            return;

        ResultsContent.Padding = new Thickness(padding.Left, top, padding.Right, bottom);
    }
}
