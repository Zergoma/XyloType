using Avalonia.Controls;

using XyloType.Desktop.Controls;
using XyloType.Desktop.Navigation;

namespace XyloType.Desktop.Views;

/// <summary>
/// The exercises section: the editor of the exercises, or the packs to import.
/// </summary>
public sealed class ExercisesView : UserControl, IViewLifecycle, INavigationGuard
{
    private readonly ExercisesManagerView _editor;
    private readonly FloatingTabs _tabs = new();

    public ExercisesView(ExercisesManagerView editor, ExercisePacksView packs)
    {
        _editor = editor;

        // an import writes the exercises of the keyboard: the edits are saved or dropped first, the editor reloads after
        packs.ViewModel.ConfirmBeforeImport = editor.ViewModel.ConfirmDiscardChangesAsync;
        packs.ViewModel.Imported += async (_, _) => await editor.ViewModel.ReloadAsync();

        _tabs.Add("Éditeur", editor);
        _tabs.Add("Packs", packs);
        Content = _tabs;
    }

    public void OnAppearing() => _tabs.OnAppearing();

    public void OnDisappearing() => _tabs.OnDisappearing();

    /// <summary>
    /// Leaving the section: the edits of the editor are saved or dropped first.
    /// </summary>
    public Task<bool> CanLeaveAsync() => _editor.CanLeaveAsync();
}
