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
            // Windows gives the focus back on its own when the focused control disappears
            // (e.g. the settings fold away): only accept a focus we asked for
            if (!IsFocusJustRequested)
            {
                Dispatcher.Dispatch(() => HiddenInput.Unfocus());
                return;
            }

            TakeFocusOverlay.IsVisible = false;
            vm.ResumeTyping();
        };

        HiddenInput.Unfocused += (_, __) =>
        {
            TakeFocusOverlay.IsVisible = true;
            vm.PauseTyping();
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
            TypingSessionResult result = vm.GetResult();

            await _navigationService.ReplaceWithStatisticAsync(result);
        }
    }

    protected override async void OnAppearing()
	{
        base.OnAppearing();

        await Task.Yield(); // laisse le layout se faire

        await Dispatcher.DispatchAsync(async () =>
        {
            await Task.Delay(100);
            RequestTypingFocus();
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
            RequestTypingFocus();
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

    // Taps bubble up to the page: ignore the page tap that follows a focus request
    private static readonly TimeSpan s_focusRequestGrace = TimeSpan.FromMilliseconds(300);
    private DateTime _lastFocusRequestUtc = DateTime.MinValue;

    private bool IsFocusJustRequested
        => DateTime.UtcNow - _lastFocusRequestUtc < s_focusRequestGrace;

    private void RequestTypingFocus()
    {
        _lastFocusRequestUtc = DateTime.UtcNow;
        HiddenInput.Focus();
    }

    private void PauseTypingFocus()
    {
        if (IsFocusJustRequested)
            return;

        // clicking outside the text pauses the typing (and its clock)
        if (HiddenInput.IsFocused)
            HiddenInput.Unfocus();
    }

    private void TakeFocusButton_Clicked(object sender, EventArgs e)
    {
        // mark the request first: folding the settings must not pause the typing
        _lastFocusRequestUtc = DateTime.UtcNow;

        // back to typing: fold the settings away (scrolls back to the top)
        SettingsExpander.IsExpanded = false;
        RequestTypingFocus();
    }

    private void TypingArea_Tapped(object sender, TappedEventArgs e)
    {
        // clicking on the text keeps typing
        RequestTypingFocus();
    }

    private void Page_Tapped(object? sender, TappedEventArgs e)
        => PauseTypingFocus();

    private async void SettingsExpander_ExpandedChanged(object? sender, CommunityToolkit.Maui.Core.ExpandedChangedEventArgs e)
    {
        // the expander header handles its own click, which never reaches Page_Tapped
        PauseTypingFocus();

        // let the expander lay out its content before scrolling
        await Task.Delay(50);

        if (e.IsExpanded)
        {
            await PageScrollView.ScrollToAsync(SettingsExpander, ScrollToPosition.End, animated: true);
        }
        else
        {
            await PageScrollView.ScrollToAsync(0, 0, animated: true);
        }
    }

    public void ScrollToCurrentLine(int index)
    {
        if (index < 0 || index >= TypingLinesLayout.Children.Count)
            return;

        if (TypingLinesLayout.Children[index] is not Element line)
            return;

        _ = TypingScrollView.ScrollToAsync(
            line,
            ScrollToPosition.Start,
            animated: true);
    }
}