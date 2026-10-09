using Avalonia.Controls;

using XyloType.ViewModels.Import;

namespace XyloType.Desktop.Views;

public partial class ImportBookView : UserControl
{
    public ImportBookView(ImportBookViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
