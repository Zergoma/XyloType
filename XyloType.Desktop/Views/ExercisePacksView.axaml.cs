using Avalonia.Controls;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.ExercisesManager;

namespace XyloType.Desktop.Views;

public partial class ExercisePacksView : UserControl, IViewLifecycle
{
    private readonly ExercisePacksViewModel _vm;

    public ExercisePacksView(ExercisePacksViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    public ExercisePacksViewModel ViewModel => _vm;

    public async void OnAppearing()
        => await _vm.LoadAsync();

    public void OnDisappearing()
    {
    }
}
