using XyloType.Navigation;
using XyloType.ViewModels.ExercisesManager;

namespace XyloType.MVVM.Views;

public partial class ExercisesManagerView : ContentView, IViewLifecycle, INavigationGuard
{
    // Vertical move needed before a press becomes a drag (a smaller move stays a click)
    private const double DragThreshold = 6;
    private const uint ShiftAnimationMs = 120;
    private const uint DropAnimationMs = 100;

    private readonly ExercisesManagerViewModel _vm;

    // drag state
    private View? _pressedItem;
    private bool _isDragging;
    private int _fromIndex;
    private int _targetIndex;
    private double _startY;
    private double _pitch;

    public ExercisesManagerView(ExercisesManagerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

#if WINDOWS
        // fixed height texts: their scrollbar shows when the text is longer
        foreach (Editor editor in new[] { EditorGeneratedText, EditorDescription })
        {
            editor.HandlerChanged += (_, _) =>
            {
                if (editor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
                    Microsoft.UI.Xaml.Controls.ScrollViewer.SetVerticalScrollBarVisibility(textBox, Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
            };
        }
#endif
    }

    public async void OnAppearing()
    {
        await _vm.InitializeAsync();
    }

    public void OnDisappearing()
    {
    }

    #region Unsaved changes

    public async Task<bool> CanLeaveAsync()
        => !_vm.HasChanges || await _vm.ConfirmDiscardChangesAsync();

    #endregion

    #region Drag and drop reorder

    private void Item_PointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not View item || !ExerciseList.Children.Contains(item))
            return;

        _pressedItem = item;
        _isDragging = false;
        _fromIndex = ExerciseList.Children.IndexOf(item);
        _targetIndex = _fromIndex;
        _startY = e.GetPosition(ExerciseList)?.Y ?? 0;
        _pitch = item.Height + item.Margin.Bottom;
    }

    private void List_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedItem is null || e.GetPosition(ExerciseList) is not Point position)
            return;

        double dy = position.Y - _startY;

        if (!_isDragging)
        {
            if (Math.Abs(dy) < DragThreshold)
                return;
            StartDrag();
        }

        int count = ExerciseList.Children.Count;

        // the item follows the cursor, kept inside the list
        double minDy = -_fromIndex * _pitch;
        double maxDy = (count - 1 - _fromIndex) * _pitch;
        _pressedItem.TranslationY = Math.Clamp(dy, minDy, maxDy);

        int target = Math.Clamp(_fromIndex + (int)Math.Round(dy / _pitch), 0, count - 1);
        if (target == _targetIndex)
            return;

        _targetIndex = target;
        ShiftOtherItems();
    }

    private void List_PointerReleased(object? sender, PointerEventArgs e)
        => _ = EndDragAsync();

    private void List_PointerExited(object? sender, PointerEventArgs e)
        => _ = EndDragAsync();

    private void StartDrag()
    {
        if (_pressedItem is null)
            return;

        _isDragging = true;
        _pressedItem.ZIndex = 10;
        _ = _pressedItem.ScaleToAsync(1.03, ShiftAnimationMs, Easing.CubicOut);
        _ = _pressedItem.FadeToAsync(0.9, ShiftAnimationMs);
    }

    /// <summary>
    /// Slides the items between the start and the target position to open a gap.
    /// </summary>
    private void ShiftOtherItems()
    {
        for (int i = 0; i < ExerciseList.Children.Count; i++)
        {
            if (i == _fromIndex || ExerciseList.Children[i] is not View child)
                continue;

            double shift =
                i > _fromIndex && i <= _targetIndex ? -_pitch
                : i < _fromIndex && i >= _targetIndex ? _pitch
                : 0;

            if (child.TranslationY != shift)
                _ = child.TranslateToAsync(0, shift, ShiftAnimationMs, Easing.CubicOut);
        }
    }

    private async Task EndDragAsync()
    {
        View? item = _pressedItem;
        bool wasDragging = _isDragging;

        _pressedItem = null;
        _isDragging = false;

        if (item is null || !wasDragging)
            return;

        int from = _fromIndex;
        int to = _targetIndex;

        // settle the item on its new slot, then commit the order
        await Task.WhenAll(
            item.TranslateToAsync(0, (to - from) * _pitch, DropAnimationMs, Easing.CubicOut),
            item.ScaleToAsync(1, DropAnimationMs),
            item.FadeToAsync(1, DropAnimationMs));

        foreach (IView child in ExerciseList.Children)
        {
            if (child is View view)
            {
                view.CancelAnimations();
                view.TranslationY = 0;
            }
        }
        item.ZIndex = 0;

        _vm.Move(from, to);
    }

    #endregion

    #region Text marks

    private static void InsertTextInEditor(string text, Editor editor)
    {
        string currentText = editor.Text ?? string.Empty;
        int cursorPosition = Math.Clamp(editor.CursorPosition, 0, currentText.Length);

        editor.Text = currentText.Insert(cursorPosition, text);
        editor.CursorPosition = cursorPosition + text.Length;
    }

    private void AddEnterMark_Clicked(object? sender, EventArgs e)
        => InsertTextInEditor("↵", EditorGeneratedText);

    private void AddTabMark_Clicked(object? sender, EventArgs e)
        => InsertTextInEditor("⟶", EditorGeneratedText);

    #endregion
}
