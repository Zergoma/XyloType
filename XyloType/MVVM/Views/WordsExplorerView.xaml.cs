using XyloType.MVVM.Controls;
using XyloType.Navigation;
using XyloType.ViewModels.WordsExplorer;

namespace XyloType.MVVM.Views;

public partial class WordsExplorerView : ContentView, IViewLifecycle
{
    private readonly WordsExplorerViewModel _vm;

    public WordsExplorerView(WordsExplorerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void OnAppearing()
    {

        // refreshed each time: an import may have added words
        await _vm.InitializeAsync();
    }

    // narrow table (small window): short headers, so they are not cut
    // narrow table (small window): short headers, so they are not cut; the columns fit the width
    private void TableHeader_SizeChanged(object? sender, EventArgs e)
    {
        _vm.IsCompactTable = TableHeader.Width > 0 && TableHeader.Width < 720;

        if (Resources["TableColumns"] is WordsTableColumns columns)
            columns.Fit(TableHeader.Width - TableHeader.Padding.HorizontalThickness);
    }

    // hovered row: outlined and shaded, on top of the excluded color
    private Border? _hoveredRow;

    private void Row_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Border row)
            return;

        // a row left through its button may have missed its exit
        if (_hoveredRow is not null && _hoveredRow != row)
            ClearHighlight(_hoveredRow);

        _hoveredRow = row;
        row.Stroke = new SolidColorBrush(Resource("Accent"));
        row.Content?.SetAppThemeColor(BackgroundColorProperty, Resource("RowHoverBgLight"), Resource("RowHoverBgDark"));
    }

    private void Row_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Border row)
            return;

        // moving onto the row button also "exits" the row: still inside, keep the highlight
        if (e.GetPosition(row) is Point position
            && position.X >= 0 && position.Y >= 0
            && position.X < row.Width && position.Y < row.Height)
            return;

        ClearHighlight(row);
        if (_hoveredRow == row)
            _hoveredRow = null;
    }

    private static void ClearHighlight(Border row)
    {
        row.Stroke = Colors.Transparent;
        if (row.Content is not null)
            row.Content.BackgroundColor = Colors.Transparent;
    }

    // a button hidden while hovered (clicked "Exclure") or recycled for another word never gets
    // its pointer exit: it would come back still in its hover color
    private void RowButton_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible))
            ResetVisualState(sender);
    }

    private void RowButton_BindingContextChanged(object? sender, EventArgs e)
        => ResetVisualState(sender);

    private static void ResetVisualState(object? sender)
    {
        if (sender is Button button)
            VisualStateManager.GoToState(button, button.IsEnabled ? "Normal" : "Disabled");
    }

    // "Exclure": red on hover, set here rather than by a visual state, which kept a stale hover
    // on the rows created while scrolling; back to neutral whenever the row shows another word
    private void ExcludeButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button button)
            button.SetAppThemeColor(BackgroundColorProperty, Resource("ButtonDangerBgHoverLight"), Resource("ButtonDangerBgHoverDark"));
    }

    private void ExcludeButton_PointerExited(object? sender, PointerEventArgs e)
        => SetNeutral(sender);

    private void ExcludeButton_BindingContextChanged(object? sender, EventArgs e)
        => SetNeutral(sender);

    private void ExcludeButton_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible))
            SetNeutral(sender);
    }

    private static void SetNeutral(object? sender)
    {
        if (sender is Button button)
            button.SetAppThemeColor(BackgroundColorProperty, Resource("ButtonNeutralBgLight"), Resource("ButtonNeutralBgDark"));
    }

    // a word cut by its column shows in full at once when hovered, over it:
    // left aligned with its text, centered on it (a tooltip of our own: the system one comes after a delay)
    // the word hovered, in the page: the tooltip lies over it and must go when the pointer leaves it
    private Rect _hoveredWord;

    private void WordLabel_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Label label || !IsCut(label))
            return;

        // where the word is in the page: the pointer, seen from the page and from the word
        if (e.GetPosition(RootGrid) is not Point inPage || e.GetPosition(label) is not Point inWord)
            return;

        _hoveredWord = new Rect(inPage.X - inWord.X, inPage.Y - inWord.Y, label.Width, label.Height);

        // the tooltip lies exactly on the outline of the row (left, top and bottom),
        // its text where the word is: the row is the border around the grid of the word
        if (label.Parent?.Parent is not Border row || e.GetPosition(row) is not Point inRow)
            return;

        Rect rowInPage = new(inPage.X - inRow.X, inPage.Y - inRow.Y, row.Width, row.Height);

        WordTooltipText.Text = label.Text;
        WordTooltip.HeightRequest = rowInPage.Height;
        WordTooltip.Padding = new Thickness(_hoveredWord.X - rowInPage.X - WordTooltip.StrokeThickness, 0, 12, 0);
        // the tooltip is laid out inside the padding of the page
        WordTooltip.TranslationX = rowInPage.X - RootGrid.Padding.Left;
        WordTooltip.TranslationY = rowInPage.Y - RootGrid.Padding.Top;
        WordTooltip.IsVisible = true;
    }

    // the tooltip, now under the pointer, makes the word "exited": it is hidden only when the pointer
    // is really out of the word
    private void WordLabel_PointerExited(object? sender, PointerEventArgs e)
        => HideWordTooltipWhenOut(e);

    private void WordTooltip_PointerMoved(object? sender, PointerEventArgs e)
        => HideWordTooltipWhenOut(e);

    private void WordTooltip_PointerExited(object? sender, PointerEventArgs e)
        => WordTooltip.IsVisible = false;

    private void HideWordTooltipWhenOut(PointerEventArgs e)
    {
        if (e.GetPosition(RootGrid) is Point inPage && _hoveredWord.Contains(inPage))
            return;

        WordTooltip.IsVisible = false;
    }

    /// <summary>
    /// Windows tells when the text of a TextBlock is cut (measuring the label in MAUI was not reliable).
    /// </summary>
    private static bool IsCut(Label label)
    {
#if WINDOWS
        return label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock text && text.IsTextTrimmed;
#else
        return false;
#endif
    }

    private static Color Resource(string key)
        => Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.Gray;

    public void OnDisappearing()
    {
    }
}
