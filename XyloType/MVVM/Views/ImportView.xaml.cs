using Xylocopadream.UI.Maui.Controls;
using XyloType.Navigation;

namespace XyloType.MVVM.Views;

public partial class ImportView : ContentView, IViewLifecycle
{
    private const string Words = "Mots";
    private const string Book = "Livre";

    private readonly Dictionary<string, View> _views;
    private View? _current;
    private bool _isShown;

    public ImportView(ImportWordView importWord, ImportBookView importBook)
    {
        InitializeComponent();

        _views = new() { [Words] = importWord, [Book] = importBook };
        foreach (View view in _views.Values)
        {
            view.IsVisible = false;
            KindHost.Children.Add(view);
        }

        KindSelector.ItemsSource = _views.Keys.ToList();
        KindSelector.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SegmentedControl.SelectedItem))
                ShowKind();
        };
        KindSelector.SelectedItem = Words;
    }

    private void ShowKind()
    {
        if (KindSelector.SelectedItem is not string kind || _current == _views[kind])
            return;

        if (_current is not null)
        {
            if (_isShown)
                (_current as IViewLifecycle)?.OnDisappearing();
            _current.IsVisible = false;
        }

        _current = _views[kind];
        _current.IsVisible = true;

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
