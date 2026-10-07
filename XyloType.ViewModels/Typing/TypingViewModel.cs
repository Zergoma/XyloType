using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Enums;
using XyloType.Domain.Music;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;
using XyloType.ViewModels.WordsExplorer;


namespace XyloType.ViewModels.Typing;

public partial class TypingViewModel : ObservableObject
{
    // standard notes, of the same scale (G6, E6, D6, C6, A5): one is picked at each correct key
    private static readonly int[] s_standardNotes = [91, 88, 86, 84, 81];

    private int _lastOkSoundIndex = -1;
    private const string KeyErrorSound = "key_error.wav";

    public event Action<int>? LineChanged;

    /// <summary>
    /// Colors of the letters (correct, wrong, current...): a theme file of the app, or of the user.
    /// </summary>
    public const string TypingThemeName = "XyloType_Typing_Theme";

    public TypingSession Session { get; } = new();
    public ObservableCollection<TypingLineStateViewModel> LinesStates { get; } = [];

    /// <summary>
    /// The lines shown on screen: a window around the current line.
    /// Every character is several UI elements: showing a long text at once is very slow.
    /// </summary>
    public ObservableCollection<TypingLineStateViewModel> VisibleLines { get; } = [];

    private const int VisibleLinesBefore = 1;
    private const int VisibleLinesAfter = 4;

    private int _firstVisibleLine;
    private int _lastVisibleLine = -1;

    private readonly ITypingThemeProvider _typingThemeProvider;
    private readonly IInputCharMapperService _charMapper;
    private readonly IThemeChangerService _themeChangerService;
    private readonly IPlaySoundSample _soundSamplePlayer;
    private readonly IUserTypingPreferenceService _typingPreference;
    private readonly IScoreCatalog _scoreCatalog;


    public TypingViewModel(
        IInputCharMapperService charMapper,
        ITypingThemeProvider typingThemeProvider,
        IThemeChangerService themeChangerService,
        IPlaySoundSample soundSamplePlayer,
        IUserTypingPreferenceService typingPreference,
        IScoreCatalog scoreCatalog)
    {
        _charMapper = charMapper;

        Session.LineChanged += (int lineNumber) =>
        {
            UpdateVisibleLines(lineNumber);
            LineChanged?.Invoke(lineNumber);
        };

        Session.HitKeyStatusChanged += Session_HitKeyStatusChanged;


        _typingThemeProvider = typingThemeProvider;
        _themeChangerService = themeChangerService;
        _soundSamplePlayer = soundSamplePlayer;
        _typingPreference = typingPreference;
        _scoreCatalog = scoreCatalog;

        _okVolume = typingPreference.GetOkVolume();
        _errorVolume = typingPreference.GetErrorVolume();

        _okSoundMode = typingPreference.GetOkSoundMode();
        _scoreEndBehavior = typingPreference.GetScoreEndBehavior();
        _scoreShuffle = typingPreference.GetScoreShuffle();
        IReadOnlySet<InstrumentChoice> disabledInstruments = typingPreference.GetDisabledInstruments();
        foreach (var (value, label, _, _) in s_instruments)
            InstrumentSwitches.Add(new InstrumentOptionViewModel(value, label, !disabledInstruments.Contains(value), OnInstrumentEnabledChanged));

        _instrumentChoice = typingPreference.GetInstrument();
        PickInstrument();
        IReadOnlySet<string> disabled = typingPreference.GetDisabledScores();
        foreach (Score score in scoreCatalog.GetAll())
            ScoreOptions.Add(new ScoreOptionViewModel(score, !disabled.Contains(score.Id), OnScoreEnabledChanged));

        BuildScoreGroups();

        Session.BackReturnEnable = typingPreference.GetBackReturnEnable();
        Session.StopOnError = typingPreference.GetStopOnError();
    }

    /// <summary>
    /// A random standard note, never the same twice in a row so the variation is heard.
    /// </summary>
    private int NextStandardNote()
    {
        int index;
        if (_lastOkSoundIndex < 0)
        {
            index = Random.Shared.Next(s_standardNotes.Length);
        }
        else
        {
            // draw among the other sounds: skip the last one
            index = Random.Shared.Next(s_standardNotes.Length - 1);
            if (index >= _lastOkSoundIndex)
                index++;
        }

        _lastOkSoundIndex = index;
        return s_standardNotes[index];
    }

    #region Correct key sound: standard notes or a piece of music

    // notes available for every instrument: <folder>/<prefix>_<midi>.wav, C5 to C7
    private const int LowestNote = 72;
    private const int HighestNote = 96;

    private OkSoundMode _okSoundMode;
    private ScoreEndBehavior _scoreEndBehavior;
    private bool _scoreShuffle;
    private InstrumentChoice _instrumentChoice;
    private InstrumentChoice _instrument;
    private Melody? _melody;

    public IReadOnlyList<PickerOption<OkSoundMode>> OkSoundModeOptions { get; } =
    [
        new("Notes", OkSoundMode.Standard),
        new("Instrumental", OkSoundMode.Instrumental),
        new("Chanson", OkSoundMode.Song),
    ];

    public PickerOption<OkSoundMode> OkSoundModeSelected
    {
        get => OkSoundModeOptions.First(o => o.Value == _okSoundMode);
        set
        {
            if (value is null || value.Value == _okSoundMode)
                return;

            _okSoundMode = value.Value;
            _typingPreference.SetOkSoundMode(_okSoundMode);

            if (IsScoreMode)
                PickScore();
            else
                _melody = null;

            _ = PreloadOkSoundsAsync();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSongMode));
            OnPropertyChanged(nameof(ScoreCategoryGroups));
            OnScoreChanged();
        }
    }

    public bool IsScoreMode => _okSoundMode is OkSoundMode.Instrumental or OkSoundMode.Song;

    /// <summary>
    /// Pieces of the current mode: songs, or instrumental pieces.
    /// </summary>
    private bool IsOfMode(Score score)
        => score.IsSong == (_okSoundMode == OkSoundMode.Song);

    private bool IsPlayable(ScoreOptionViewModel option)
        => option.IsEnabled && IsOfMode(option.Score);

    // every playable instrument, in display order
    private static readonly (InstrumentChoice Value, string Label, string Folder, string Prefix)[] s_instruments =
    [
        (InstrumentChoice.Xylophone, "Xylophone (synthèse)", "xylophone", "xylo"),
        (InstrumentChoice.XylophoneRecorded, "Xylophone (enregistré)", "xylophone2", "xylo2"),
        (InstrumentChoice.Piano, "Piano", "piano", "piano"),
        (InstrumentChoice.Marimba, "Marimba", "marimba", "marimba"),
        (InstrumentChoice.Vibraphone, "Vibraphone", "vibraphone", "vibraphone"),
        (InstrumentChoice.Glockenspiel, "Glockenspiel", "glockenspiel", "glock"),
    ];

    public IReadOnlyList<PickerOption<InstrumentChoice>> InstrumentOptions { get; } =
        [.. s_instruments.Select(i => new PickerOption<InstrumentChoice>(i.Label, i.Value)),
         new("Au hasard", InstrumentChoice.Random)];

    /// <summary>
    /// Every instrument, with a switch to leave it out of the random pick.
    /// </summary>
    public ObservableCollection<InstrumentOptionViewModel> InstrumentSwitches { get; } = [];

    public PickerOption<InstrumentChoice> InstrumentSelected
    {
        get => InstrumentOptions.First(o => o.Value == _instrumentChoice);
        set
        {
            if (value is null || value.Value == _instrumentChoice)
                return;

            SetInstrumentChoice(value.Value);
            PickInstrument();
            _ = PreloadOkSoundsAsync();
        }
    }

    public bool IsRandomInstrument => _instrumentChoice == InstrumentChoice.Random;

    /// <summary>
    /// The instrument playing now, shown under the text.
    /// </summary>
    public string CurrentInstrumentLabel
        => s_instruments.First(i => i.Value == _instrument).Label;

    private void SetInstrumentChoice(InstrumentChoice choice)
    {
        _instrumentChoice = choice;
        _typingPreference.SetInstrument(choice);
        OnPropertyChanged(nameof(InstrumentSelected));
        OnPropertyChanged(nameof(IsRandomInstrument));
    }

    private InstrumentChoice[] EnabledInstruments()
        => [.. InstrumentSwitches.Where(s => s.IsEnabled).Select(s => s.Value)];

    /// <summary>
    /// Resolves "Au hasard" to one of the enabled instruments (at each exercise),
    /// another one than the current instrument when possible.
    /// </summary>
    private void PickInstrument(bool another = false)
    {
        InstrumentChoice previous = _instrument;

        if (_instrumentChoice != InstrumentChoice.Random)
        {
            _instrument = _instrumentChoice;
        }
        else
        {
            InstrumentChoice[] enabled = EnabledInstruments();
            if (another && enabled.Length > 1)
                enabled = [.. enabled.Where(i => i != _instrument)];

            _instrument = enabled.Length == 0
                ? InstrumentChoice.Xylophone
                : enabled[Random.Shared.Next(enabled.Length)];
        }

        RememberInstrument(previous);
        OnPropertyChanged(nameof(CurrentInstrumentLabel));
    }

    // instruments played before, to go back with "previous"
    private readonly Stack<InstrumentChoice> _instrumentHistory = new();
    private bool _hasInstrument;

    private void RememberInstrument(InstrumentChoice previous)
    {
        if (_hasInstrument && previous != _instrument)
            _instrumentHistory.Push(previous);

        _hasInstrument = true;
        OnPropertyChanged(nameof(HasPreviousInstrument));
    }

    public bool HasPreviousInstrument => _instrumentHistory.Any(IsInstrumentEnabled);

    private bool IsInstrumentEnabled(InstrumentChoice instrument)
        => InstrumentSwitches.Any(s => s.Value == instrument && s.IsEnabled);

    /// <summary>
    /// Goes back to the instrument played before (it becomes the chosen one when an instrument was chosen).
    /// </summary>
    [RelayCommand]
    public void PreviousInstrument()
    {
        while (_instrumentHistory.TryPop(out InstrumentChoice instrument))
        {
            if (!IsInstrumentEnabled(instrument))
                continue;

            if (_instrumentChoice != InstrumentChoice.Random)
                SetInstrumentChoice(instrument);

            _instrument = instrument;
            OnPropertyChanged(nameof(CurrentInstrumentLabel));
            OnPropertyChanged(nameof(HasPreviousInstrument));
            _ = PreloadOkSoundsAsync();
            return;
        }

        OnPropertyChanged(nameof(HasPreviousInstrument));
    }

    /// <summary>
    /// Plays with another instrument right now: another random one,
    /// or the next one of the list when an instrument was chosen.
    /// </summary>
    [RelayCommand]
    public void ChangeInstrument()
    {
        if (_instrumentChoice == InstrumentChoice.Random)
        {
            PickInstrument(another: true);
        }
        else
        {
            InstrumentChoice[] enabled = EnabledInstruments();
            if (enabled.Length == 0)
                return;

            int index = Array.IndexOf(enabled, _instrumentChoice);
            SetInstrumentChoice(enabled[(index + 1) % enabled.Length]);
            PickInstrument();
        }

        _ = PreloadOkSoundsAsync();
    }

    /// <summary>
    /// Never uses the current instrument again (left out of the random pick), and goes on with another one.
    /// An instrument chosen in the settings switches to "Au hasard". The last enabled instrument stays.
    /// </summary>
    [RelayCommand]
    public void ExcludeCurrentInstrument()
    {
        if (EnabledInstruments().Length <= 1)
            return;

        InstrumentOptionViewModel? current = InstrumentSwitches.FirstOrDefault(s => s.Value == _instrument);
        current?.IsEnabled = false; // saves the preference and picks another instrument
    }

    private void OnInstrumentEnabledChanged(InstrumentOptionViewModel option)
    {
        // keep at least one instrument
        if (!option.IsEnabled && EnabledInstruments().Length == 0)
        {
            option.IsEnabled = true;
            return;
        }

        _typingPreference.SetDisabledInstruments(InstrumentSwitches.Where(s => !s.IsEnabled).Select(s => s.Value));
        OnPropertyChanged(nameof(HasPreviousInstrument));

        if (!option.IsEnabled && option.Value == _instrument)
        {
            if (_instrumentChoice == option.Value)
                SetInstrumentChoice(InstrumentChoice.Random);

            PickInstrument(another: true);
            _ = PreloadOkSoundsAsync();
        }
    }

    /// <summary>
    /// Goes on with another piece right now.
    /// </summary>
    [RelayCommand]
    public void SkipScore()
    {
        if (!IsScoreMode)
            return;

        PickScore();
        OnScoreChanged();
    }

    /// <summary>
    /// Never plays the current piece again (it is disabled in the settings), and goes on with another one.
    /// </summary>
    [RelayCommand]
    public void ExcludeCurrentScore()
    {
        if (_melody is null)
            return;

        ScoreOptionViewModel? option = ScoreOptions.FirstOrDefault(o => o.Score.Id == _melody.Score.Id);
        option?.IsEnabled = false; // saves the preference and picks another piece
    }

    // shown under the text while a piece is played
    public bool HasCurrentScore => _melody is not null;
    public string CurrentScoreTitle => _melody?.Score.Title ?? string.Empty;
    public string CurrentScoreComposer => _melody?.Score.Composer ?? string.Empty;

    /// <summary>
    /// Tooltip of the piece: song or instrumental, and its composer.
    /// </summary>
    public string CurrentScoreDetails
        => _melody is null ? string.Empty : $"{(_melody.Score.IsSong ? "Chanson" : "Instrumental")} · {_melody.Score.Composer}";

    public bool IsCurrentScoreSong => _melody?.Score.IsSong ?? false;

    public IReadOnlyList<PickerOption<ScoreEndBehavior>> ScoreEndOptions { get; } =
    [
        new("Recommencer le morceau", ScoreEndBehavior.Loop),
        new("Passer à un autre morceau", ScoreEndBehavior.NextScore),
    ];

    public PickerOption<ScoreEndBehavior> ScoreEndSelected
    {
        get => ScoreEndOptions.First(o => o.Value == _scoreEndBehavior);
        set
        {
            if (value is null || value.Value == _scoreEndBehavior)
                return;

            _scoreEndBehavior = value.Value;
            _typingPreference.SetScoreEndBehavior(_scoreEndBehavior);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Every piece, with a switch to leave it out of the random pick.
    /// </summary>
    public ObservableCollection<ScoreOptionViewModel> ScoreOptions { get; } = [];

    /// <summary>
    /// Settings: the instrumental pieces grouped by kind of music, in display order.
    /// </summary>
    public IReadOnlyList<ScoreCategoryViewModel> InstrumentalScoreGroups { get; private set; } = [];

    /// <summary>
    /// Settings: the songs grouped by kind of music, in display order.
    /// </summary>
    public IReadOnlyList<ScoreCategoryViewModel> SongScoreGroups { get; private set; } = [];

    /// <summary>
    /// The groups of the current mode.
    /// </summary>
    public IReadOnlyList<ScoreCategoryViewModel> ScoreCategoryGroups => IsSongMode ? SongScoreGroups : InstrumentalScoreGroups;

    public bool IsSongMode => _okSoundMode == OkSoundMode.Song;

    /// <summary>
    /// Both lists are built once and the view only shows one of them: rebuilding the list at each change of mode
    /// removed views (switches, expanders) still bound to the pieces, and WinUI threw on their disconnected handlers.
    /// </summary>
    private void BuildScoreGroups()
    {
        InstrumentalScoreGroups = GroupByCategory(isSong: false);
        SongScoreGroups = GroupByCategory(isSong: true);
    }

    private ScoreCategoryViewModel[] GroupByCategory(bool isSong)
        => [.. ScoreCategories.DisplayOrder
            .Select(category => ScoreOptions.Where(o => o.Score.Category == category && o.Score.IsSong == isSong).ToArray())
            .Where(pieces => pieces.Length > 0)
            .Select(pieces => new ScoreCategoryViewModel(pieces[0].Score.Category, pieces))];

    public string CurrentScoreText
        => _melody is null
            ? "Aucun morceau activé : notes standard"
            : $"♪ {_melody.Score.Title} — {_melody.Score.Composer}";

    /// <summary>
    /// Picks a random piece among the enabled ones (none enabled: standard notes),
    /// another one than the current piece when possible.
    /// </summary>
    private void PickScore()
    {
        Score? score = _scoreShuffle ? RandomScore() : NextScoreInOrder();

        Melody? next = score is null
            ? null
            : new Melody(score, LowestNote, HighestNote);

        PlayScore(next);
    }


    private Score? RandomScore()
    {
        ScoreOptionViewModel[] enabled = [.. ScoreOptions.Where(IsPlayable)];
        if (enabled.Length > 1 && _melody is not null)
            enabled = [.. enabled.Where(o => o.Score.Id != _melody.Score.Id)];

        return enabled.Length == 0 ? null : enabled[Random.Shared.Next(enabled.Length)].Score;
    }

    /// <summary>
    /// The first enabled piece after the current one, in the catalog order (looping).
    /// </summary>
    private Score? NextScoreInOrder()
    {
        int count = ScoreOptions.Count;
        int current = _melody is null
            ? -1
            : ScoreOptions.IndexOf(ScoreOptions.First(o => o.Score.Id == _melody.Score.Id));

        return Enumerable
            .Range(1, count)
            .Select(step => ScoreOptions[(current + step + count) % count])
            .FirstOrDefault(IsPlayable)
            ?.Score;
    }

    public bool IsScoreShuffle => _scoreShuffle;

    /// <summary>
    /// Random order of the pieces, or the catalog order.
    /// </summary>
    [RelayCommand]
    public void ToggleScoreShuffle()
    {
        _scoreShuffle = !_scoreShuffle;
        _typingPreference.SetScoreShuffle(_scoreShuffle);
        OnPropertyChanged(nameof(IsScoreShuffle));
    }

    /// <summary>
    /// Random instrument (changing with each piece), or the current instrument kept.
    /// </summary>
    [RelayCommand]
    public void ToggleRandomInstrument()
    {
        SetInstrumentChoice(IsRandomInstrument ? _instrument : InstrumentChoice.Random);
    }
    // pieces played before, to go back with "previous"
    private readonly Stack<Score> _scoreHistory = new();

    private void PlayScore(Melody? melody, bool remember = true)
    {
        if (remember && _melody is not null && _melody.Score.Id != melody?.Score.Id)
            _scoreHistory.Push(_melody.Score);

        bool changed = _melody?.Score.Id != melody?.Score.Id;
        _melody = melody;
        OnPropertyChanged(nameof(HasPreviousScore));

        // random instrument: a new instrument with each new piece
        if (changed && melody is not null && IsRandomInstrument)
        {
            PickInstrument(another: true);
            _ = PreloadOkSoundsAsync();
        }
    }

    public bool HasPreviousScore => _scoreHistory.Any(IsScoreEnabled);

    private bool IsScoreEnabled(Score score)
        => ScoreOptions.Any(o => o.Score.Id == score.Id && IsPlayable(o));

    /// <summary>
    /// Goes back to the piece played before (from its beginning).
    /// </summary>
    [RelayCommand]
    public void PreviousScore()
    {
        while (_scoreHistory.TryPop(out Score? score))
        {
            if (!IsScoreEnabled(score))
                continue;

            PlayScore(new Melody(score, LowestNote, HighestNote), remember: false);
            OnScoreChanged();
            return;
        }

        OnPropertyChanged(nameof(HasPreviousScore));
    }

    private void OnScoreEnabledChanged(ScoreOptionViewModel option)
    {
        _typingPreference.SetDisabledScores(ScoreOptions.Where(o => !o.IsEnabled).Select(o => o.Score.Id));
        OnPropertyChanged(nameof(HasPreviousScore));

        // the piece being played was just disabled, or nothing was playing: pick again
        if (IsScoreMode && (_melody is null || _melody.Score.Id == option.Score.Id && !option.IsEnabled))
        {
            PickScore();
            _ = PreloadOkSoundsAsync();
            OnScoreChanged();
        }
    }

    private void OnScoreChanged()
    {
        UpdateScoreChangeMarker();
        OnPropertyChanged(nameof(ScoreProgress));
        OnPropertyChanged(nameof(IsScoreMode));
        OnPropertyChanged(nameof(CurrentScoreText));
        OnPropertyChanged(nameof(HasCurrentScore));
        OnPropertyChanged(nameof(CurrentScoreTitle));
        OnPropertyChanged(nameof(CurrentScoreComposer));
        OnPropertyChanged(nameof(CurrentScoreDetails));
        OnPropertyChanged(nameof(IsCurrentScoreSong));
    }

    private string NextOkSoundFile()
    {
        if (_melody is null)
            return NoteFile(_instrument, NextStandardNote());

        string file = NoteFile(_instrument, _melody.NextNote());

        // the piece is over: start it again (the melody loops) or go on with another one
        if (_melody.IsAtStart && _scoreEndBehavior == ScoreEndBehavior.NextScore)
        {
            PickScore();
            OnScoreChanged();
        }

        return file;
    }

    private static string NoteFile(InstrumentChoice instrument, int midi)
    {
        var (_, _, folder, prefix) = s_instruments.First(i => i.Value == instrument);
        return $"{folder}/{prefix}_{midi}.wav";
    }

    /// <summary>
    /// A low, "fat" note of the instrument (the synthesized xylophone keeps its marimba-like sound).
    /// </summary>
    private static string ErrorFile(InstrumentChoice instrument)
    {
        if (instrument == InstrumentChoice.Xylophone)
            return KeyErrorSound;

        var (_, _, folder, prefix) = s_instruments.First(i => i.Value == instrument);
        return $"{folder}/{prefix}_error.wav";
    }

    // every note of the instrument: the next piece starts without any delay
    private Task PreloadOkSoundsAsync()
        => _soundSamplePlayer.PreloadAsync(
            [.. Enumerable.Range(LowestNote, HighestNote - LowestNote + 1).Select(midi => NoteFile(_instrument, midi)), ErrorFile(_instrument)]);

    #endregion

    private void Session_HitKeyStatusChanged(HitKeyStatus hitKeyStatus)
    {
        if (hitKeyStatus == HitKeyStatus.Success)
        {
            _soundSamplePlayer.PlaySound(NextOkSoundFile(), OkVolume);
            OnPropertyChanged(nameof(ScoreProgress));
            _speedMeter.Record(Session.Duration);
            RefreshLiveSpeed();
        }
        else
        {
            _soundSamplePlayer.PlaySound(ErrorFile(_instrument), ErrorVolume);
        }
    }

    private double _okVolume;
    public double OkVolume
    {
        get => _okVolume;
        set
        {
            if (!SetProperty(ref _okVolume, value))
                return;
            _typingPreference.SetOkVolume(value);
            OnPropertyChanged(nameof(OkVolumeTxt));
        }
    }

    public string OkVolumeTxt
        => $"{OkVolume:P0}";

    private double _errorVolume;
    public double ErrorVolume
    {
        get => _errorVolume;
        set
        {
            if (!SetProperty(ref _errorVolume, value))
                return;
            _typingPreference.SetErrorVolume(value);
            OnPropertyChanged(nameof(ErrorVolumeTxt));
        }
    }

    public string ErrorVolumeTxt
        => $"{ErrorVolume:P0}";

    [RelayCommand]
    public void PreviewOkSound()
        => _soundSamplePlayer.PlaySound(NextOkSoundFile(), OkVolume);

    [RelayCommand]
    public void PreviewErrorSound()
        => _soundSamplePlayer.PlaySound(ErrorFile(_instrument), ErrorVolume);

    public bool StopOnErrorEnable
    {
        get => Session.StopOnError;
        set
        {
            if (Session.StopOnError == value)
                return;
            Session.StopOnError = value;
            _typingPreference.SetStopOnError(value);
            OnPropertyChanged(nameof(StopOnErrorEnable));
            OnPropertyChanged(nameof(StopOnErrorTxt));
        }
    }

    [RelayCommand]
    public async Task SwitchStopOnError()
        => StopOnErrorEnable = !StopOnErrorEnable;

    public string StopOnErrorTxt
        => StopOnErrorEnable ? "Arret sur erreur" : "Continue sur erreur";

    public bool BackReturnEnable
    {
        get => Session.BackReturnEnable;
        set
        {
            if (Session.BackReturnEnable == value)
                return;

            Session.BackReturnEnable = value;
            _typingPreference.SetBackReturnEnable(value);
            OnPropertyChanged(nameof(BackReturnEnable));
            OnPropertyChanged(nameof(BackReturnTxt));
        }
    }

    [RelayCommand]
    public async Task SwitchBackReturn()
        => BackReturnEnable = !BackReturnEnable;

    public string BackReturnTxt
        => BackReturnEnable ? "Retour arrière activé" : "Retour arrière interdit";

    public bool ShowSpeedResult
    {
        get => _typingPreference.GetShowSpeedResult();
        set
        {
            if (ShowSpeedResult == value)
                return;

            _typingPreference.SetShowSpeedResult(value);
            OnPropertyChanged(nameof(ShowSpeedResult));
        }
    }

    [RelayCommand]
    public void SwitchShowSpeedResult()
        => ShowSpeedResult = !ShowSpeedResult;

    public bool ShowResponseTimeResult
    {
        get => _typingPreference.GetShowResponseTimeResult();
        set
        {
            if (ShowResponseTimeResult == value)
                return;

            _typingPreference.SetShowResponseTimeResult(value);
            OnPropertyChanged(nameof(ShowResponseTimeResult));
        }
    }

    [RelayCommand]
    public void SwitchShowResponseTimeResult()
        => ShowResponseTimeResult = !ShowResponseTimeResult;

    public bool ShowErrorsResult
    {
        get => _typingPreference.GetShowErrorsResult();
        set
        {
            if (ShowErrorsResult == value)
                return;

            _typingPreference.SetShowErrorsResult(value);
            OnPropertyChanged(nameof(ShowErrorsResult));
        }
    }

    [RelayCommand]
    public void SwitchShowErrorsResult()
        => ShowErrorsResult = !ShowErrorsResult;

    public async Task<Result<bool>> LoadTextAsync(IStringsProvider stringProvider)
    {
        Session.Lines.Clear();
        LinesStates.Clear();
        VisibleLines.Clear();
        _firstVisibleLine = 0;
        _lastVisibleLine = -1;

        // "Au hasard": a new instrument for each exercise
        PickInstrument();

        if (IsScoreMode)
            PickScore();

        // the notes are decoded meanwhile, off the UI thread: the exercise shows without waiting for them
        _ = PreloadOkSoundsAsync();

        // Get current theme apply
        ThemeState themeState = _themeChangerService.GetTheme();

        // TODO
        // to property, + user access
        Result <ITypingTheme> themeResu =
            await _typingThemeProvider.GetThemeAsync(TypingThemeName, themeState);

        if (!themeResu.Success)
        {
            return Result<bool>.Fail(themeResu.Error);
        }

        Result<IEnumerable<string>> getStringResult =
            await stringProvider.GetStringsAsync();

        if (!getStringResult.Success)
        {
            return Result<bool>.Fail(getStringResult.Error);
        }

        string[] dataLines = [.. getStringResult.GetValue];

        if (dataLines.Length == 0)
            return Result<bool>.Fail("L'exercice ne contient aucun texte");

        foreach (string line in dataLines)
        {
            TypingLine typingLine = new(line);
            Session.Lines.Add(typingLine);

            TypingLineStateViewModel typingLineState = new(themeResu.GetValue, typingLine);
            LinesStates.Add(typingLineState);
        }

        _speedMeter.Reset();
        OnPropertyChanged(nameof(TypingProgress));
        Session.ResetProgression();
        UpdateVisibleLines(Session.CurrentLineIndex);
        SettleVisibleLines(Session.CurrentLineIndex);
        UpdateScoreChangeMarker();

        return Result<bool>.Ok(true);
    }


    /// <summary>
    /// Adapts the window of visible lines to the current line, below it only:
    /// adding or removing lines under the current one does not move the text on screen.
    /// The lines above are handled by the view (<see cref="RemoveTopLines"/>, <see cref="InsertTopLines"/>),
    /// so it can keep the text in place while it scrolls.
    /// </summary>
    private void UpdateVisibleLines(int currentLine)
    {
        if (LinesStates.Count == 0)
            return;

        int first = FirstWantedLine(currentLine);
        int last = Math.Min(LinesStates.Count - 1, currentLine + VisibleLinesAfter);

        // no overlap with the current window (first load, big jump): rebuild it
        if (VisibleLines.Count == 0 || currentLine < _firstVisibleLine || currentLine > _lastVisibleLine + 1)
        {
            VisibleLines.Clear();
            for (int i = first; i <= last; i++)
                VisibleLines.Add(LinesStates[i]);

            _firstVisibleLine = first;
            _lastVisibleLine = last;
            return;
        }

        while (_lastVisibleLine > last)
        {
            VisibleLines.RemoveAt(VisibleLines.Count - 1);
            _lastVisibleLine--;
        }

        while (_lastVisibleLine < last)
        {
            _lastVisibleLine++;
            VisibleLines.Add(LinesStates[_lastVisibleLine]);
        }
    }

    private static int FirstWantedLine(int currentLine)
        => Math.Max(0, currentLine - VisibleLinesBefore);

    /// <summary>
    /// Lines above the window that are no longer needed (they scrolled out of sight).
    /// </summary>
    public int TopLinesToRemove(int currentLine)
        => Math.Max(0, Math.Min(FirstWantedLine(currentLine), _lastVisibleLine) - _firstVisibleLine);

    /// <summary>
    /// Lines missing above the window (going back with backspace).
    /// </summary>
    public int TopLinesToInsert(int currentLine)
        => Math.Max(0, _firstVisibleLine - FirstWantedLine(currentLine));

    public void RemoveTopLines(int count)
    {
        for (int i = 0; i < count && VisibleLines.Count > 1; i++)
        {
            VisibleLines.RemoveAt(0);
            _firstVisibleLine++;
        }
    }

    public void InsertTopLines(int count)
    {
        for (int i = 0; i < count && _firstVisibleLine > 0; i++)
        {
            _firstVisibleLine--;
            VisibleLines.Insert(0, LinesStates[_firstVisibleLine]);
        }
    }

    /// <summary>
    /// Applies the top changes at once (no animation).
    /// </summary>
    public void SettleVisibleLines(int currentLine)
    {
        RemoveTopLines(TopLinesToRemove(currentLine));
        InsertTopLines(TopLinesToInsert(currentLine));
    }

    /// <summary>
    /// Position of a line in <see cref="VisibleLines"/>, -1 if not shown.
    /// </summary>
    public int VisibleIndexOf(int lineNumber)
        => lineNumber >= _firstVisibleLine && lineNumber <= _lastVisibleLine
            ? lineNumber - _firstVisibleLine
            : -1;

    public TypingStatus ProcessInput(char input)
    {
        TypingStatus status = Session.ProcessInput(input, _charMapper.Map);
        OnPropertyChanged(nameof(TypingProgress));
        UpdateScoreChangeMarker();
        return status;
    }

    /// <summary>
    /// Back to the start of the text (F5).
    /// </summary>
    public void ResetProgression()
    {
        Session.ResetProgression();
        _speedMeter.Reset();
        OnPropertyChanged(nameof(TypingProgress));
        UpdateScoreChangeMarker();
        RefreshLiveSpeed();
    }

    /// <summary>
    /// The app theme changed during the exercise: the letters take the colors of the new theme.
    /// </summary>
    public async Task RefreshTypingThemeAsync()
    {
        Result<ITypingTheme> theme =
            await _typingThemeProvider.GetThemeAsync(TypingThemeName, _themeChangerService.GetTheme());
        if (!theme.Success)
            return;

        foreach (TypingLineStateViewModel line in LinesStates)
            line.ApplyTheme(theme.GetValue);
    }

    public void PauseTyping()
        => Session.Pause();

    /// <summary>
    /// No key for a while: the user is doing something else, the exercise pauses (idle time taken off the clocks).
    /// True when it just paused.
    /// </summary>
    public bool PauseIfInactive()
        => Session.PauseIfInactive();

    public void ResumeTyping()
        => Session.Resume();


    public TypingSessionResult GetResult()
        => Session.GetResult();

    #region Progress and live speed

    private readonly TypingSpeedMeter _speedMeter = new(TimeSpan.FromSeconds(10));

    /// <summary>
    /// Part of the text already typed, from 0 to 1.
    /// </summary>
    public double TypingProgress => Session.Progress;

    /// <summary>
    /// Part of the piece of music played, from 0 to 1.
    /// </summary>
    public double ScoreProgress => _melody?.Progress ?? 0;

    [ObservableProperty]
    public partial string LiveSpeedText { get; set; } = NoSpeedText;

    private const string NoSpeedText = "— mots/min";

    /// <summary>
    /// Words per minute over the last 10 seconds. Called after each key and regularly by the view,
    /// so the speed falls when the typing stops.
    /// </summary>
    public void RefreshLiveSpeed()
    {
        double? wordsPerMinute = _speedMeter.WordsPerMinute(Session.Duration);
        LiveSpeedText = wordsPerMinute is double speed ? $"{speed:0} mots/min" : NoSpeedText;
    }

    public bool ShowTypingProgress
    {
        get => _typingPreference.GetShowTypingProgress();
        set
        {
            if (ShowTypingProgress == value)
                return;

            _typingPreference.SetShowTypingProgress(value);
            OnPropertyChanged(nameof(ShowTypingProgress));
        }
    }

    public bool ShowLiveSpeed
    {
        get => _typingPreference.GetShowLiveSpeed();
        set
        {
            if (ShowLiveSpeed == value)
                return;

            _typingPreference.SetShowLiveSpeed(value);
            OnPropertyChanged(nameof(ShowLiveSpeed));
        }
    }

    public bool ShowScoreProgress
    {
        get => _typingPreference.GetShowScoreProgress();
        set
        {
            if (ShowScoreProgress == value)
                return;

            _typingPreference.SetShowScoreProgress(value);
            OnPropertyChanged(nameof(ShowScoreProgress));
        }
    }

    [RelayCommand]
    public void SwitchShowTypingProgress()
        => ShowTypingProgress = !ShowTypingProgress;

    [RelayCommand]
    public void SwitchShowLiveSpeed()
        => ShowLiveSpeed = !ShowLiveSpeed;

    [RelayCommand]
    public void SwitchShowScoreProgress()
        => ShowScoreProgress = !ShowScoreProgress;

    public bool ShowScoreChangeMarker
    {
        get => _typingPreference.GetShowScoreChangeMarker();
        set
        {
            if (ShowScoreChangeMarker == value)
                return;

            _typingPreference.SetShowScoreChangeMarker(value);
            OnPropertyChanged(nameof(ShowScoreChangeMarker));
            UpdateScoreChangeMarker();
        }
    }

    [RelayCommand]
    public void SwitchShowScoreChangeMarker()
        => ShowScoreChangeMarker = !ShowScoreChangeMarker;

    /// <summary>
    /// Letter where the piece of music changes (or starts again): line and column, or null.
    /// A single mark drawn by the view over the letter (one per letter cost too much to create).
    /// </summary>
    public (int Line, int Column)? ScoreChangeLetter { get; private set; }

    /// <summary>
    /// Marks the letter where the piece of music changes (or starts again), when every key is right:
    /// as many letters ahead as notes left in the melody.
    /// </summary>
    private void UpdateScoreChangeMarker()
    {
        (int Line, int Column)? letter = null;

        if (ShowScoreChangeMarker && _melody is not null && LinesStates.Count > 0)
        {
            int line = Session.CurrentLineIndex;
            int column = Session.CurrentCharacterIndex + _melody.RemainingNotes;

            while (line < LinesStates.Count && column >= LinesStates[line].Characters.Count)
            {
                column -= LinesStates[line].Characters.Count;
                line++;
            }

            if (line < LinesStates.Count)
                letter = (line, column);
        }

        if (letter == ScoreChangeLetter)
            return;

        ScoreChangeLetter = letter;
        OnPropertyChanged(nameof(ScoreChangeLetter));
    }

    #endregion

    #region Settings sections

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTypingSection))]
    [NotifyPropertyChangedFor(nameof(IsDisplaySection))]
    [NotifyPropertyChangedFor(nameof(IsSoundSection))]
    [NotifyPropertyChangedFor(nameof(SettingsSectionOption))]
    public partial SettingsSection SettingsSectionSelected { get; set; } = SettingsSection.Typing;

    public bool IsTypingSection => SettingsSectionSelected == SettingsSection.Typing;
    public bool IsDisplaySection => SettingsSectionSelected == SettingsSection.Display;
    public bool IsSoundSection => SettingsSectionSelected == SettingsSection.Sound;

    public IReadOnlyList<PickerOption<SettingsSection>> SettingsSectionOptions { get; } =
    [
        new("Frappe", SettingsSection.Typing),
        new("Affichage", SettingsSection.Display),
        new("Sons et musique", SettingsSection.Sound),
    ];

    public PickerOption<SettingsSection> SettingsSectionOption
    {
        get => SettingsSectionOptions.First(o => o.Value == SettingsSectionSelected);
        set
        {
            if (value is not null)
                SettingsSectionSelected = value.Value;
        }
    }

    #endregion
}

/// <summary>
/// The settings are shown one section at a time.
/// </summary>
public enum SettingsSection
{
    Typing,
    Display,
    Sound
}
