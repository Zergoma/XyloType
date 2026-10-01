using VM = XyloType.ViewModels.Import;

namespace XyloType.MVVM.Views;

public partial class ImportWordView : ContentPage
{
    private readonly VM.ImportWordViewModel _vm;

	public ImportWordView(VM.ImportWordViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadHistoryAsync();
    }
}
