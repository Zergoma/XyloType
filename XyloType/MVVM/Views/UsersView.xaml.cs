using XyloType.Navigation;
using XyloType.ViewModels.Users;

namespace XyloType.MVVM.Views;

public partial class UsersView : ContentView, IViewLifecycle
{
    private readonly UsersViewModel _vm;

    public UsersView(UsersViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void OnAppearing()
    {
        await _vm.RefreshAsync();

        // nobody yet: the name is the only thing to do
        if (_vm.HasNoUser)
            NewUserEntry.Focus();
    }

    public void OnDisappearing()
    {
    }
}
