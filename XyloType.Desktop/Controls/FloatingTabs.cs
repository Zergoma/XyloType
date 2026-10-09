using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using XyloType.Desktop.Navigation;

namespace XyloType.Desktop.Controls;

/// <summary>
/// A few views shown one at a time, chosen with a selector floating at the top (the sections Exercices and Import):
/// the views keep their state, and are told when they appear (<see cref="IViewLifecycle"/>, forwarded).
/// The views start their content low enough to leave room for the selector.
/// </summary>
public sealed class FloatingTabs : UserControl, IViewLifecycle
{
    private readonly ContentControl _host = new();
    private readonly SegmentedControl _selector = new() { Background = Brushes.Transparent, Padding = new Thickness(0) };
    private readonly List<(string Name, Control View)> _tabs = [];
    private Control? _current;
    private bool _isShown;

    public FloatingTabs()
    {
        Border floating = new()
        {
            Child = _selector,
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 14, 0, 0),
            BoxShadow = BoxShadows.Parse("0 4 14 0 #30000000"),
        };
        floating.Classes.Add("card");

        Content = new Panel { Children = { _host, floating } };
        _selector.SelectionChanged += (_, _) => Show();
    }

    /// <summary>
    /// Adds a view, the first one shown at once.
    /// </summary>
    public void Add(string name, Control view)
    {
        _tabs.Add((name, view));
        _selector.ItemsSource = _tabs.Select(t => t.Name).ToList();
        _selector.SelectedItem ??= _tabs[0].Name;
    }

    private void Show()
    {
        if (_selector.SelectedItem is not string name || _tabs.Find(t => t.Name == name).View is not Control view || view == _current)
            return;

        if (_isShown)
            (_current as IViewLifecycle)?.OnDisappearing();

        _current = view;
        _host.Content = view;

        if (_isShown)
            (_current as IViewLifecycle)?.OnAppearing();
    }

    public void OnAppearing()
    {
        _isShown = true;
        (_current as IViewLifecycle)?.OnAppearing();
    }

    public void OnDisappearing()
    {
        _isShown = false;
        (_current as IViewLifecycle)?.OnDisappearing();
    }
}
