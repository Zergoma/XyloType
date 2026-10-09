using XyloType.Navigation;
using XyloType.ViewModels.ExercisesManager;

namespace XyloType.MVVM.Views;

public partial class ExercisePacksView : ContentView, IViewLifecycle
{
    private readonly ExercisePacksViewModel _vm;

    public ExercisePacksView(ExercisePacksViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public ExercisePacksViewModel ViewModel => _vm;

    public async void OnAppearing()
    {
        await _vm.LoadAsync();
    }

    public void OnDisappearing()
    {
    }
}
