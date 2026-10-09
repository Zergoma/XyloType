using Avalonia.Controls;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.Import;

namespace XyloType.Desktop.Views;

public partial class ImportWordView : UserControl, IViewLifecycle
{
    private readonly ImportWordViewModel _vm;

    public ImportWordView(ImportWordViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    public async void OnAppearing()
    {
        await _vm.LoadHistoryAsync();
        await _vm.WordPacks.LoadAsync();
    }

    public void OnDisappearing()
    {
    }
}
