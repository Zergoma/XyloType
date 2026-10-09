using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using XyloType.Desktop.Navigation;
using XyloType.ViewModels.Users;

namespace XyloType.Desktop.Views;

public partial class UsersView : UserControl, IViewLifecycle
{
    private readonly UsersViewModel _vm;
    private readonly AppNavigator _navigator;

    public UsersView(UsersViewModel vm, AppNavigator navigator)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        _navigator = navigator;
    }

    public async void OnAppearing()
    {
        await _vm.RefreshAsync();

        // nobody yet: the name is the only thing to do
        if (_vm.HasNoUser)
            NewUserBox.Focus();
    }

    public void OnDisappearing()
    {
    }

    private async void Close_Click(object? sender, RoutedEventArgs e)
        => await _navigator.CloseUsersAsync();

    private void NewUserBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.CreateCommand.CanExecute(null))
        {
            e.Handled = true;
            _vm.CreateCommand.Execute(null);
        }
    }
}
