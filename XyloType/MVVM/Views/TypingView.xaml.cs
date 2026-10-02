using XyloType.Navigation;
using Microsoft.UI.Xaml.Input;

using XyloType.Application.Interfaces;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;
using XyloType.ViewModels.Typing;


namespace XyloType.MVVM.Views;

public partial class TypingView : ContentView, IViewLifecycle
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
            _targetLine = lineNumber;
            Dispatcher.Dispatch(() => _ = FollowCurrentLineAsync(vm));
        };

        // the speed and progress follow the letter being typed
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TypingViewModel.TypingProgress)
                or nameof(TypingViewModel.ShowTypingProgress)
                or nameof(TypingViewModel.ShowLiveSpeed))
                Dispatcher.Dispatch(() => PlaceCaretInfo(animated: true));
        };
        TypingLinesLayout.SizeChanged += (_, _) => PlaceCaretInfo(animated: false);
        
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

    public async void OnAppearing()
	{

        StartLiveSpeedTimer();

        await Task.Yield(); // laisse le layout se faire

        await Dispatcher.DispatchAsync(async () =>
        {
            await Task.Delay(100);
            RequestTypingFocus();
        });
    }

    public void OnDisappearing()
    {
        _liveSpeedTimer?.Stop();
    }

    // the live speed also falls when no key is typed
    private IDispatcherTimer? _liveSpeedTimer;

    private void StartLiveSpeedTimer()
    {
        if (_liveSpeedTimer is null)
        {
            _liveSpeedTimer = Dispatcher.CreateTimer();
            _liveSpeedTimer.Interval = TimeSpan.FromMilliseconds(500);
            _liveSpeedTimer.Tick += (_, _) =>
            {
                if (BindingContext is TypingViewModel vm && vm.ShowLiveSpeed)
                    vm.RefreshLiveSpeed();
            };
        }

        _liveSpeedTimer.Start();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
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
                vm.ResetProgression();
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

    private void TakeFocusButton_Clicked(object? sender, EventArgs e)
    {
        // mark the request first: folding the settings must not pause the typing
        _lastFocusRequestUtc = DateTime.UtcNow;

        // back to typing: fold the settings away (scrolls back to the top)
        SettingsExpander.IsExpanded = false;
        RequestTypingFocus();
    }

    // the piece buttons take the focus when clicked: give it back so the typing goes on

    private void SkipScore_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.SkipScoreCommand.Execute(null);

        RequestTypingFocus();
    }

    private void ExcludeScore_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.ExcludeCurrentScoreCommand.Execute(null);

        RequestTypingFocus();
    }

    private void ToggleScoreShuffle_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.ToggleScoreShuffleCommand.Execute(null);

        RequestTypingFocus();
    }

    private void ToggleRandomInstrument_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.ToggleRandomInstrumentCommand.Execute(null);

        RequestTypingFocus();
    }

    // volume sliders: hear the new volume when the slider is released
    private void OkVolume_DragCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.PreviewOkSoundCommand.Execute(null);
    }

    private void ErrorVolume_DragCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.PreviewErrorSoundCommand.Execute(null);
    }

    private void PreviousScore_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.PreviousScoreCommand.Execute(null);

        RequestTypingFocus();
    }

    private void PreviousInstrument_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.PreviousInstrumentCommand.Execute(null);

        RequestTypingFocus();
    }

    private void ChangeInstrument_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.ChangeInstrumentCommand.Execute(null);

        RequestTypingFocus();
    }

    private void ExcludeInstrument_Clicked(object? sender, EventArgs e)
    {
        if (BindingContext is TypingViewModel vm)
            vm.ExcludeCurrentInstrumentCommand.Execute(null);

        RequestTypingFocus();
    }

    private void TypingArea_Tapped(object? sender, TappedEventArgs e)
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

    #region Smooth scrolling between lines

    // time for the layout to measure the lines just added
    private static readonly TimeSpan s_layoutDelay = TimeSpan.FromMilliseconds(30);

    private int _targetLine;
    private bool _isFollowingLine;

    /// <summary>
    /// Scrolls smoothly to the current line, then updates the lines above it
    /// while keeping the text still on screen.
    /// When typing fast, the animations do not pile up: they go to the latest line.
    /// </summary>
    private async Task FollowCurrentLineAsync(TypingViewModel vm)
    {
        if (_isFollowingLine)
            return;

        _isFollowingLine = true;
        try
        {
            int line;
            do
            {
                line = _targetLine;
                await Task.Delay(s_layoutDelay);

                // going back: the previous lines come back above, the text must not move
                int toInsert = vm.TopLinesToInsert(line);
                if (toInsert > 0)
                {
                    double heightBefore = TypingLinesLayout.Height;
                    vm.InsertTopLines(toInsert);
                    await Task.Delay(s_layoutDelay);
                    double added = TypingLinesLayout.Height - heightBefore;
                    await TypingScrollView.ScrollToAsync(0, TypingScrollView.ScrollY + added, animated: false);
                }

                await ScrollToLineAsync(vm.VisibleIndexOf(line));

                // the lines that went out of sight above are removed, the text must not move
                int toRemove = vm.TopLinesToRemove(line);
                if (toRemove > 0)
                {
                    double removed = TypingLinesLayout.Children
                        .Take(toRemove)
                        .OfType<View>()
                        .Sum(child => child.Height);

                    vm.RemoveTopLines(toRemove);
                    await TypingScrollView.ScrollToAsync(0, Math.Max(0, TypingScrollView.ScrollY - removed), animated: false);
                }
            }
            while (line != _targetLine);
        }
        finally
        {
            _isFollowingLine = false;
        }

        // the lines above were removed: once laid out again, the letter has moved up
        await Task.Delay(s_layoutDelay);
        PlaceCaretInfo(animated: false);
    }

    private async Task ScrollToLineAsync(int index)
    {
        if (index < 0 || index >= TypingLinesLayout.Children.Count)
            return;

        if (TypingLinesLayout.Children[index] is not Element line)
            return;

        await TypingScrollView.ScrollToAsync(
            line,
            ScrollToPosition.Start,
            animated: true);
    }

    #endregion

    #region Speed and progress under the current letter

    private void TypingScrollView_Scrolled(object? sender, ScrolledEventArgs e)
        => PlaceCaretInfo(animated: false);

    /// <summary>
    /// Puts the live speed and the small progress bar just under the letter being typed,
    /// where the eyes are. Hidden when the letter is out of sight.
    /// </summary>
    private void PlaceCaretInfo(bool animated)
    {
        if (BindingContext is not TypingViewModel vm || !(vm.ShowTypingProgress || vm.ShowLiveSpeed))
        {
            CaretInfo.Opacity = 0;
            return;
        }

        int lineIndex = vm.VisibleIndexOf(vm.Session.CurrentLineIndex);
        if (lineIndex < 0 || lineIndex >= TypingLinesLayout.Children.Count
            || TypingLinesLayout.Children[lineIndex] is not Layout line || line.Children.Count == 0)
        {
            CaretInfo.Opacity = 0;
            return;
        }

        // at the very end the cursor is past the last letter: stay under it
        int charIndex = Math.Min(vm.Session.CurrentCharacterIndex, line.Children.Count - 1);
        if (line.Children[charIndex] is not View letter || letter.Width <= 0)
            return;

        double x = line.Frame.X + letter.Frame.X + letter.Frame.Width / 2 - CaretInfo.WidthRequest / 2;
        double y = line.Frame.Y + letter.Frame.Bottom - TypingScrollView.ScrollY + 1;

        x = Math.Clamp(x, 0, Math.Max(0, TypingScrollView.Width - CaretInfo.WidthRequest));

        if (y < 0 || y > TypingScrollView.Height - CaretInfo.Height)
        {
            CaretInfo.Opacity = 0;
            return;
        }

        CaretInfo.CancelAnimations();

        if (animated && CaretInfo.Opacity > 0)
        {
            _ = CaretInfo.TranslateToAsync(x, y, 90, Easing.CubicOut);
        }
        else
        {
            CaretInfo.TranslationX = x;
            CaretInfo.TranslationY = y;
        }

        CaretInfo.Opacity = 1;
    }

    #endregion
}
