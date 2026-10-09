using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace XyloType.Desktop.Controls;

/// <summary>
/// One choice among a few, as pills side by side (the SegmentedControl of the MAUI library):
/// a list with <c>ItemsSource</c> and <c>SelectedItem</c>, whose colored pill slides to the choice.
/// The look is in Resources/Styles.axaml (PART_Indicator behind the items).
/// </summary>
public class SegmentedControl : ListBox
{
    private static readonly TimeSpan s_slideDuration = TimeSpan.FromMilliseconds(220);

    private Border? _indicator;
    private readonly Transitions _slide = SlideTransitions();
    private Rect _target;

    protected override Type StyleKeyOverride => typeof(SegmentedControl);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _indicator = e.NameScope.Find<Border>("PART_Indicator");
        _target = default;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // a choice that changes nothing else on screen brings no layout: the pill follows it anyway
        if (change.Property == SelectedIndexProperty)
            Dispatcher.UIThread.Post(MoveIndicator, DispatcherPriority.Background);
    }

    // when the items move (wrap, resize, content): the pill stays under the selected item
    private void OnLayoutUpdated(object? sender, EventArgs e) => MoveIndicator();

    private void MoveIndicator()
    {
        if (_indicator?.Parent is not Visual surface)
            return;

        if (SelectedIndex < 0
            || ContainerFromIndex(SelectedIndex) is not Control item
            || item.Bounds.Width <= 0
            || item.TranslatePoint(default, surface) is not Point at)
        {
            _indicator.IsVisible = false;
            _target = default;
            return;
        }

        Rect target = new(at, item.Bounds.Size);
        if (target == _target)
            return;

        // shown for the first time: in place at once, it slides only from one choice to another
        bool slide = _indicator.IsVisible && _target != default;
        _target = target;

        if (!slide)
            _indicator.Transitions = null;

        Canvas.SetLeft(_indicator, target.X);
        Canvas.SetTop(_indicator, target.Y);
        _indicator.Width = target.Width;
        _indicator.Height = target.Height;
        _indicator.IsVisible = true;

        if (!slide)
            Dispatcher.UIThread.Post(() => _indicator.Transitions = _slide, DispatcherPriority.Background);
    }

    private static Transitions SlideTransitions()
    {
        CubicEaseOut easing = new();
        return
        [
            new DoubleTransition { Property = Canvas.LeftProperty, Duration = s_slideDuration, Easing = easing },
            new DoubleTransition { Property = Canvas.TopProperty, Duration = s_slideDuration, Easing = easing },
            new DoubleTransition { Property = WidthProperty, Duration = s_slideDuration, Easing = easing },
            new DoubleTransition { Property = HeightProperty, Duration = s_slideDuration, Easing = easing },
        ];
    }
}
