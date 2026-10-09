using Avalonia.Controls;

using XyloType.Desktop.Controls;
using XyloType.Desktop.Navigation;

namespace XyloType.Desktop.Views;

/// <summary>
/// The import section: words (packs ready to use, or a text), or a book.
/// </summary>
public sealed class ImportView : UserControl, IViewLifecycle
{
    private readonly FloatingTabs _tabs = new();

    public ImportView(ImportWordView words, ImportBookView book)
    {
        _tabs.Add("Mots", words);
        _tabs.Add("Livre", book);
        Content = _tabs;
    }

    public void OnAppearing() => _tabs.OnAppearing();

    public void OnDisappearing() => _tabs.OnDisappearing();
}
