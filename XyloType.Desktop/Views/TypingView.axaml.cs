using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using XyloType.Application.Interfaces;
using XyloType.Desktop.Navigation;
using XyloType.Domain.Typing;
using XyloType.ViewModels.Typing;

namespace XyloType.Desktop.Views;

/// <summary>
/// The typing screen. The view itself takes the keyboard focus: the letters come composed by the system
/// (capitals, accents of the dead keys, AltGr) through <see cref="OnTextInput"/>, the special keys through KeyDown.
/// Losing the focus (a click elsewhere, a while without typing) pauses the typing and its clock.
/// </summary>
public partial class TypingView : UserControl, IViewLifecycle
{
    // every half second: the live speed (it also falls when no key is typed), and the pause when idle
    private static readonly TimeSpan s_tick = TimeSpan.FromMilliseconds(500);

    // time for the layout to measure the lines just added or removed
    private static readonly TimeSpan s_layoutDelay = TimeSpan.FromMilliseconds(30);

    private static readonly TimeSpan s_scrollDuration = TimeSpan.FromMilliseconds(160);

    private readonly TypingViewModel _vm;
    private readonly INavigationService _navigation;
    private readonly DispatcherTimer _timer;

    private bool _ended;
    private int _targetLine;
    private bool _isFollowingLine;

    public TypingView(TypingViewModel vm, INavigationService navigation)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        _navigation = navigation;

        _timer = new DispatcherTimer { Interval = s_tick };
        _timer.Tick += (_, _) => OnTick();

        // the special keys before anything else uses them (Tab would move the focus)
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

        // a click outside the text pauses the typing, even on a spot that takes no focus
        AddHandler(PointerPressedEvent, OnPointerPressedAnywhere, RoutingStrategies.Tunnel, handledEventsToo: true);

        // the volume is heard when its slider is released, or moved with the arrows
        // (the slider handles the pointer itself: its events come even when already handled)
        PreviewVolumeOnRelease(OkVolumeSlider, _vm.PreviewOkSound);
        PreviewVolumeOnRelease(ErrorVolumeSlider, _vm.PreviewErrorSound);

        vm.LineChanged += line =>
        {
            _targetLine = line;
            Dispatcher.UIThread.Post(() => _ = FollowCurrentLineAsync());
        };

        // the speed and progress follow the letter being typed
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TypingViewModel.TypingProgress)
                or nameof(TypingViewModel.ShowTypingProgress)
                or nameof(TypingViewModel.ShowLiveSpeed)
                or nameof(TypingViewModel.ScoreChangeLetter))
                Dispatcher.UIThread.Post(PlaceCaretInfo, DispatcherPriority.Background);
        };
        LinesList.SizeChanged += (_, _) => PlaceCaretInfo();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == IsFocusedProperty)
                OnTypingFocusChanged(IsFocused);
        };
    }

    #region Lifecycle

    public void OnAppearing()
    {
        if (global::Avalonia.Application.Current is { } app)
            app.ActualThemeVariantChanged += OnThemeChanged;

        _timer.Start();

        // once laid out
        Dispatcher.UIThread.Post(RequestTypingFocus, DispatcherPriority.Loaded);
    }

    public void OnDisappearing()
    {
        _timer.Stop();

        if (global::Avalonia.Application.Current is { } app)
            app.ActualThemeVariantChanged -= OnThemeChanged;
    }

    // the letters already shown take the colors of the new theme
    private async void OnThemeChanged(object? sender, EventArgs e)
        => await _vm.RefreshTypingThemeAsync();

    private void OnTick()
    {
        // no key for a while: the user is doing something else, the typing pauses
        // (the focus goes to "Reprendre la saisie", Enter or Space take it back)
        if (_vm.PauseIfInactive() && IsFocused)
        {
            TakeFocusOverlay.IsVisible = true;
            TakeFocusButton.Focus();
        }

        if (_vm.ShowLiveSpeed)
            _vm.RefreshLiveSpeed();
    }

    #endregion

    #region Keys

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!IsFocused || string.IsNullOrEmpty(e.Text))
            return;

        e.Handled = true;
        foreach (char input in e.Text)
        {
            // the control keys come through KeyDown
            if (!char.IsControl(input))
                Process(input);
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsFocused)
            return;

        switch (e.Key)
        {
            case Key.Back:
                e.Handled = true;
                Process('\b');
                break;

            case Key.Enter:
                e.Handled = true;
                Process('\n');
                break;

            case Key.Tab:
                e.Handled = true;
                Process('\t');
                break;

            case Key.F5:
                e.Handled = true;
                _vm.ResetProgression();
                break;
        }
    }

    private async void Process(char input)
    {
        if (_ended || _vm.ProcessInput(input) != TypingStatus.Ended)
            return;

        // the text is typed: the results take its place
        _ended = true;
        _timer.Stop();
        await _navigation.ReplaceWithStatisticAsync(_vm.GetResult());
    }

    #endregion

    #region Focus: typing or paused

    private void RequestTypingFocus()
    {
        if (!_ended)
            Focus();
    }

    private void OnTypingFocusChanged(bool focused)
    {
        TakeFocusOverlay.IsVisible = !focused;

        if (focused)
            _vm.ResumeTyping();
        else
            _vm.PauseTyping();
    }

    private void OnPointerPressedAnywhere(object? sender, PointerPressedEventArgs e)
    {
        // the text and the music buttons keep typing
        if (!IsFocused || e.Source is not Visual source
            || TypingArea.IsVisualAncestorOf(source) || MusicBar.IsVisualAncestorOf(source))
            return;

        // once the click is done: a control clicked has the focus (the typing is already paused),
        // an empty spot gave it back to the view itself
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsFocused)
                return;

            TakeFocusOverlay.IsVisible = true;
            TakeFocusButton.Focus();
        }, DispatcherPriority.Input);
    }

    // a click on the text keeps typing
    private void TypingArea_PointerPressed(object? sender, PointerPressedEventArgs e)
        => RequestTypingFocus();

    private void TakeFocusOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
        => TakeFocus_Click(sender, e);

    // back to typing: the settings fold away
    private void TakeFocus_Click(object? sender, RoutedEventArgs e)
    {
        SettingsExpander.IsExpanded = false;
        RequestTypingFocus();
    }

    private void SettingsExpander_Changed(object? sender, RoutedEventArgs e)
    {
        // the settings are shown whole, or the text again at the top
        if (SettingsExpander.IsExpanded)
            Dispatcher.UIThread.Post(() => PageScroll.ScrollToEnd(), DispatcherPriority.Background);
        else
            PageScroll.ScrollToHome();
    }

    #endregion

    #region Volume

    private static void PreviewVolumeOnRelease(Slider slider, Action preview)
    {
        slider.AddHandler(PointerReleasedEvent, (_, _) => preview(), RoutingStrategies.Bubble, handledEventsToo: true);
        slider.AddHandler(KeyUpEvent, (_, e) =>
        {
            if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown)
                preview();
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    #endregion

    #region Music: the buttons give the focus back, the typing goes on

    private void Run(System.Windows.Input.ICommand command)
    {
        command.Execute(null);
        RequestTypingFocus();
    }

    private void ToggleScoreShuffle_Click(object? sender, RoutedEventArgs e) => Run(_vm.ToggleScoreShuffleCommand);

    private void PreviousScore_Click(object? sender, RoutedEventArgs e) => Run(_vm.PreviousScoreCommand);

    private void SkipScore_Click(object? sender, RoutedEventArgs e) => Run(_vm.SkipScoreCommand);

    private void ExcludeScore_Click(object? sender, RoutedEventArgs e) => Run(_vm.ExcludeCurrentScoreCommand);

    private void ToggleRandomInstrument_Click(object? sender, RoutedEventArgs e) => Run(_vm.ToggleRandomInstrumentCommand);

    private void PreviousInstrument_Click(object? sender, RoutedEventArgs e) => Run(_vm.PreviousInstrumentCommand);

    private void ChangeInstrument_Click(object? sender, RoutedEventArgs e) => Run(_vm.ChangeInstrumentCommand);

    private void ExcludeInstrument_Click(object? sender, RoutedEventArgs e) => Run(_vm.ExcludeCurrentInstrumentCommand);

    #endregion

    #region Smooth scrolling between lines

    /// <summary>
    /// Scrolls smoothly to the current line, then updates the lines above it while keeping the text still on screen.
    /// When typing fast, the scrolls do not pile up: they go to the latest line.
    /// </summary>
    private async Task FollowCurrentLineAsync()
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
                int toInsert = _vm.TopLinesToInsert(line);
                if (toInsert > 0)
                {
                    double heightBefore = LinesList.Bounds.Height;
                    _vm.InsertTopLines(toInsert);
                    await Task.Delay(s_layoutDelay);
                    LinesScroll.Offset = LinesScroll.Offset.WithY(LinesScroll.Offset.Y + LinesList.Bounds.Height - heightBefore);
                }

                if (LineContainer(_vm.VisibleIndexOf(line)) is Control target)
                    await ScrollToAsync(target.Bounds.Y);

                // the lines that went out of sight above are removed, the text must not move
                int toRemove = _vm.TopLinesToRemove(line);
                if (toRemove > 0)
                {
                    double removed = Enumerable.Range(0, toRemove)
                        .Select(LineContainer)
                        .Sum(container => container?.Bounds.Height ?? 0);

                    _vm.RemoveTopLines(toRemove);
                    LinesScroll.Offset = LinesScroll.Offset.WithY(Math.Max(0, LinesScroll.Offset.Y - removed));
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
        PlaceCaretInfo();
    }

    /// <summary>
    /// Eased scroll of the text to <paramref name="y"/>.
    /// </summary>
    private async Task ScrollToAsync(double y)
    {
        double from = LinesScroll.Offset.Y;
        double to = Math.Clamp(y, 0, Math.Max(0, LinesScroll.Extent.Height - LinesScroll.Viewport.Height));
        if (Math.Abs(to - from) < 1)
            return;

        DateTime start = DateTime.UtcNow;
        double progress;
        do
        {
            progress = Math.Min(1, (DateTime.UtcNow - start) / s_scrollDuration);
            double eased = 1 - Math.Pow(1 - progress, 3);
            LinesScroll.Offset = LinesScroll.Offset.WithY(from + (to - from) * eased);
            await Task.Delay(15);
        }
        while (progress < 1);
    }

    private Control? LineContainer(int index)
        => index >= 0 && index < LinesList.ItemCount ? LinesList.ContainerFromIndex(index) : null;

    /// <summary>
    /// The letter at <paramref name="column"/> of the visible line <paramref name="lineIndex"/>, if laid out.
    /// </summary>
    private Control? Letter(int lineIndex, int column)
    {
        if (LineContainer(lineIndex) is not ContentPresenter { Child: ItemsControl letters } || letters.ItemCount == 0)
            return null;

        Control? letter = letters.ContainerFromIndex(Math.Clamp(column, 0, letters.ItemCount - 1));
        return letter is { Bounds.Width: > 0 } ? letter : null;
    }

    #endregion

    #region Marks over the text

    private void LinesScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        => PlaceCaretInfo();

    /// <summary>
    /// Puts the live speed and the small progress bar just under the letter being typed, where the eyes are,
    /// and the mark of the letter where the piece of music changes. Hidden when out of sight.
    /// </summary>
    private void PlaceCaretInfo()
    {
        PlaceScoreChangeMark();

        if (!(_vm.ShowTypingProgress || _vm.ShowLiveSpeed)
            || Letter(_vm.VisibleIndexOf(_vm.Session.CurrentLineIndex), _vm.Session.CurrentCharacterIndex) is not Control letter
            || letter.TranslatePoint(new Point(letter.Bounds.Width / 2, letter.Bounds.Height), Overlay) is not Point under)
        {
            CaretInfo.Opacity = 0;
            return;
        }

        double x = Math.Clamp(under.X - CaretInfo.Width / 2, 0, Math.Max(0, Overlay.Bounds.Width - CaretInfo.Width));
        if (under.Y < 0 || under.Y > Overlay.Bounds.Height - CaretInfo.Bounds.Height)
        {
            CaretInfo.Opacity = 0;
            return;
        }

        Canvas.SetLeft(CaretInfo, x);
        Canvas.SetTop(CaretInfo, under.Y + 1);
        CaretInfo.Opacity = 1;
    }

    private void PlaceScoreChangeMark()
    {
        ScoreChangeMark.Opacity = 0;

        if (_vm.ScoreChangeLetter is not var (lineNumber, column)
            || Letter(_vm.VisibleIndexOf(lineNumber), column) is not Control letter
            || letter.TranslatePoint(new Point(0, 0), Overlay) is not Point corner
            || corner.Y < 0 || corner.Y > Overlay.Bounds.Height - letter.Bounds.Height)
            return;

        Canvas.SetLeft(ScoreChangeMark, corner.X);
        Canvas.SetTop(ScoreChangeMark, corner.Y);
        ScoreChangeMark.Opacity = 0.75;
    }

    #endregion
}
