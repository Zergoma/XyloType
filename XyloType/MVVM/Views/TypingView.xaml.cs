using Microsoft.UI.Xaml.Input;

using XyloType.Application.Interfaces;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;
using XyloType.ViewModels.Typing;


namespace XyloType.MVVM.Views;

public partial class TypingView : ContentPage
{
    private event Action TextEnded;
    private readonly INavigationService _navigationService;
    private TypingStatus Status
    {
        get;
        set
        {
            field = value;
            if(field == TypingStatus.Ended)
            {
                TextEnded?.Invoke();
            }
        }
    }

    public TypingView(
        TypingViewModel vm,
        INavigationService navigationService)
    {
        InitializeComponent();
        BindingContext = vm;

        TextEnded += OnTextEndDetected;

        #region Keyboard Focus

        HiddenInput.Focused += (_, __) =>
        {
            TakeFocusButton.IsVisible = false;
        };

        HiddenInput.Unfocused += (_, __) =>
        {
            TakeFocusButton.IsVisible = true;
        };
        #endregion

        vm.LineChanged += (int lineNumber) =>
        {
            Dispatcher.DispatchAsync(async () =>
            {
                await Task.Delay(50);

                ScrollToCurrentLine(lineNumber);
            });
        };
        
        _navigationService = navigationService;
    }

    private async void OnTextEndDetected()
    {
        if (BindingContext is TypingViewModel vm)
        {
            Dictionary<char, CharStats> stat = vm.GetTotalCharStats();

            // TODO
            // Make it better
            await _navigationService.PopBackAsync();
            await _navigationService.NavigateToStatisticAsync(stat);
        }
    }

    protected override async void OnAppearing()
	{
        base.OnAppearing();

        await Task.Yield(); // laisse le layout se faire

        await Dispatcher.DispatchAsync(async () =>
        {
            await Task.Delay(100);
            HiddenInput.Focus();
        });
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is not TypingViewModel vm)
        {
            return;
        }

        if (string.IsNullOrEmpty(e.NewTextValue))
        {
            return;
        }
        
        char input = e.NewTextValue[^1];
        HiddenInput.Text = string.Empty;

        Status = vm.ProcessInput(input);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (HiddenInput.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox nativeTextBox)
            return;

        nativeTextBox.KeyDown -= OnNativeKeyDown;
        nativeTextBox.KeyDown += OnNativeKeyDown;

        Dispatcher.Dispatch(() =>
        {
            HiddenInput.Focus();
        });
    }

    private void OnNativeKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (BindingContext is not TypingViewModel vm)
            return;

        var key = e.Key;

        switch (key)
        {
            case Windows.System.VirtualKey.Back:
                e.Handled = true;
                Status = vm.ProcessInput('\b');
                break;

            case Windows.System.VirtualKey.Enter:
                e.Handled = true;
                HiddenInput.Text = string.Empty;
                Status = vm.ProcessInput('\n');
                break;

            case Windows.System.VirtualKey.F5:
                e.Handled = true;
                vm.Session.ResetProgression();
                break;

            case Windows.System.VirtualKey.Tab:
                e.Handled = true;
                Status = vm.ProcessInput('\t');
                break;
        }
    }

    private void TakeFocusButton_Clicked(object sender, EventArgs e)
    {
        HiddenInput.Focus();
    }

    public void ScrollToCurrentLine(int index)
    {
        if(index <0 )
            return;

        TypingCollectionView.ScrollTo(
            index,
            position: ScrollToPosition.Start,
            animate: true);
    }
}