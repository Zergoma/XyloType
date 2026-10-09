using Avalonia.Controls;
using Avalonia.Threading;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.WordsExplorer;

namespace XyloType.Desktop.Views;

/// <summary>
/// The imported words: filters on the left, the table on the right, loaded by pages as it scrolls.
/// </summary>
public partial class WordsExplorerView : UserControl, IViewLifecycle
{
    // the next page is loaded when the end of the list is this close
    private const double LoadMoreDistance = 400;

    // narrow table (small window): short headers, so they are not cut
    private const double CompactTableWidth = 720;

    private readonly WordsExplorerViewModel _vm;

    public WordsExplorerView(WordsExplorerViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;

        TableHeader.SizeChanged += (_, e) => _vm.IsCompactTable = e.NewSize.Width < CompactTableWidth;

        // the scroll viewer inside the list tells when it scrolls
        Rows.AddHandler(ScrollViewer.ScrollChangedEvent, (_, e) =>
        {
            if (e.Source is ScrollViewer scroll)
                LoadMoreIfNearEnd(scroll);
        });
    }

    // refreshed each time: an import may have added words
    public async void OnAppearing()
        => await _vm.InitializeAsync();

    public void OnDisappearing()
    {
    }

    private void LoadMoreIfNearEnd(ScrollViewer scroll)
    {
        if (scroll.Extent.Height - scroll.Offset.Y - scroll.Viewport.Height < LoadMoreDistance
            && _vm.LoadMoreCommand.CanExecute(null))
            Dispatcher.UIThread.Post(() => _vm.LoadMoreCommand.Execute(null));
    }
}
