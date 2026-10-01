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
}
