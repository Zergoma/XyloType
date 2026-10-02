using VM = XyloType.ViewModels.Import;

namespace XyloType.MVVM.Views;

public partial class ImportBookView : ContentView
{
	public ImportBookView(VM.ImportBookViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

    }
}