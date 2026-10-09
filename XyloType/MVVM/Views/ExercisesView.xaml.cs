using Xylocopadream.UI.Maui.Controls;

using XyloType.Navigation;

namespace XyloType.MVVM.Views;

/// <summary>
/// The exercises section: the editor of the exercises, or the packs to import.
/// </summary>
public partial class ExercisesView : ContentView, IViewLifecycle, INavigationGuard
{
    private const string Editor = "Éditeur";
    private const string Packs = "Packs";

    private readonly ExercisesManagerView _editor;
    private readonly Dictionary<string, View> _views;
    private View? _current;
    private bool _isShown;

    public ExercisesView(ExercisesManagerView editor, ExercisePacksView packs)
    {
        InitializeComponent();

        _editor = editor;
        _views = new() { [Editor] = editor, [Packs] = packs };
        foreach (View view in _views.Values)
        {
            view.IsVisible = false;
            KindHost.Children.Add(view);
        }

        // an import writes the exercises of the keyboard: the edits are saved or dropped first, the editor reloads after
        packs.ViewModel.ConfirmBeforeImport = editor.ViewModel.ConfirmDiscardChangesAsync;
        packs.ViewModel.Imported += async (_, _) => await editor.ViewModel.ReloadAsync();

        KindSelector.ItemsSource = _views.Keys.ToList();
        KindSelector.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SegmentedControl.SelectedItem))
                ShowKind();
        };
        KindSelector.SelectedItem = Editor;
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

    /// <summary>
    /// Leaving the section: the edits of the editor are saved or dropped first.
    /// </summary>
    public Task<bool> CanLeaveAsync()
        => _editor.CanLeaveAsync();
}
