using XyloType.Navigation;
using VM = XyloType.ViewModels.Import;

namespace XyloType.MVVM.Views;

public partial class ImportWordView : ContentView, IViewLifecycle
{
    private readonly VM.ImportWordViewModel _vm;

	public ImportWordView(VM.ImportWordViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
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
