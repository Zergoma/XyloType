using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.ExercisesManager;

namespace XyloType.Desktop.Views;

/// <summary>
/// The editor of the exercises: sections, exercises (reordered by their grip), the form of the one selected.
/// </summary>
public partial class ExercisesManagerView : UserControl, IViewLifecycle, INavigationGuard
{
    private readonly ExercisesManagerViewModel _vm;

    public ExercisesManagerView(ExercisesManagerViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    public ExercisesManagerViewModel ViewModel => _vm;

    public async void OnAppearing()
        => await _vm.InitializeAsync();

    public void OnDisappearing()
    {
    }

    public async Task<bool> CanLeaveAsync()
        => !_vm.HasChanges || await _vm.ConfirmDiscardChangesAsync();

    // a click on an exercise selects it (its grip drags it)
    private void Exercise_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ExerciseListItemViewModel item })
            _vm.SelectCommand.Execute(item);
    }

    #region Marks of the special keys

    private void AddEnterMark_Click(object? sender, RoutedEventArgs e)
        => Insert("↵");

    private void AddTabMark_Click(object? sender, RoutedEventArgs e)
        => Insert("⟶");

    /// <summary>
    /// Inserts a mark where the caret is, within the limit of the text.
    /// </summary>
    private void Insert(string mark)
    {
        string text = TextEditor.Text ?? string.Empty;
        if (TextEditor.MaxLength > 0 && text.Length + mark.Length > TextEditor.MaxLength)
            return;

        int caret = Math.Clamp(TextEditor.CaretIndex, 0, text.Length);
        TextEditor.Text = text.Insert(caret, mark);
        TextEditor.CaretIndex = caret + mark.Length;
        TextEditor.Focus();
    }

    #endregion
}
