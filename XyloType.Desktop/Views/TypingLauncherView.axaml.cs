using Avalonia.Controls;
using Avalonia.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Desktop.Navigation;
using XyloType.ViewModels.TypingLauncher;

namespace XyloType.Desktop.Views;

/// <summary>
/// Home: the exercises by section, the one selected, and its launch (a double click on a tile, or Enter, launches it).
/// </summary>
public partial class TypingLauncherView : UserControl, IViewLifecycle
{
    private readonly TypingLauncherViewModel _vm;
    private readonly IUserKeyboardLayoutPreferenceService _keyboardPreference;

    public TypingLauncherView(TypingLauncherViewModel vm, IUserKeyboardLayoutPreferenceService keyboardPreference)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        _keyboardPreference = keyboardPreference;

        // Enter launches the exercise selected, even when a tile has the focus (it would select it again)
        AddHandler(KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // another keyboard: remembered, and its exercises shown
        vm.KeyboardLayoutChanged += async keyboardId =>
        {
            _keyboardPreference.SetKeyboardType(keyboardId);
            await _vm.InitilizationAsync(keyboardId);
        };
    }

    public async void OnAppearing()
    {
        // no keyboard chosen yet (first start): the first one
        Result<int> saved = _keyboardPreference.GetKeyboardType();
        int keyboardId = saved.Success ? saved.GetValue : (int)_vm.KeyboardLayoutAvailable[0].KeyBoardCode;

        await _vm.InitilizationAsync(keyboardId);
    }

    public void OnDisappearing()
    {
    }

    private void Tile_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_vm.LaunchCommand.CanExecute(null))
            _vm.LaunchCommand.Execute(null);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // not while typing a number of lines or words, nor in the list of keyboards
        if (e.Key != Key.Enter || e.Source is TextBox or ComboBox || !_vm.LaunchCommand.CanExecute(null))
            return;

        e.Handled = true;
        _vm.LaunchCommand.Execute(null);
    }
}
