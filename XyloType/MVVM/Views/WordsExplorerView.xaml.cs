using XyloType.ViewModels.WordsExplorer;

namespace XyloType.MVVM.Views;

public partial class WordsExplorerView : ContentPage
{
    private readonly WordsExplorerViewModel _vm;

    public WordsExplorerView(WordsExplorerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // refreshed each time: an import may have added words
        await _vm.InitializeAsync();
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
        row.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Resource("Primary")), new SolidColorBrush(Resource("PrimaryDark")));
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

    private static Color Resource(string key)
        => Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.Gray;
}
