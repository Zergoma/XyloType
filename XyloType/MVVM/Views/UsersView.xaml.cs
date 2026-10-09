using XyloType.Navigation;
using XyloType.ViewModels.Users;

namespace XyloType.MVVM.Views;

public partial class UsersView : ContentView, IViewLifecycle
{
    private readonly UsersViewModel _vm;
    private readonly AppNavigator _navigator;

    public UsersView(UsersViewModel vm, AppNavigator navigator)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _navigator = navigator;
    }

    private async void Close_Clicked(object? sender, EventArgs e)
        => await _navigator.CloseUsersAsync();

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
