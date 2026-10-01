using XyloType.MVVM.ViewModels;

namespace XyloType.MVVM.Views;

public partial class StatisticView : ContentPage
{
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
