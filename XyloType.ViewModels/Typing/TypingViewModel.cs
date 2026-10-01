using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Enums;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;


namespace XyloType.ViewModels.Typing;

public partial class TypingViewModel : ObservableObject
{
    // xylophone notes of the same scale (G6, E6, D6, C6, A5): one is picked at each correct key
    private static readonly string[] s_keyOkSounds =
        ["key_ok.wav", "key_ok_2.wav", "key_ok_3.wav", "key_ok_4.wav", "key_ok_5.wav"];

    private int _lastOkSoundIndex = -1;
    private const string KeyErrorSound = "key_error.wav";

    public event Action<int>? LineChanged;

    public TypingSession Session { get; } = new();
    public ObservableCollection<TypingLineStateViewModel> LinesStates { get; } = [];

    private readonly ITypingThemeProvider _typingThemeProvider;
    private readonly IInputCharMapperService _charMapper;
    private readonly IThemeChangerService _themeChangerService;
    private readonly IPlaySoundSample _soundSamplePlayer;
    private readonly IUserTypingPreferenceService _typingPreference;


    public TypingViewModel(
        IInputCharMapperService charMapper,
        ITypingThemeProvider typingThemeProvider,
        IThemeChangerService themeChangerService,
        IPlaySoundSample soundSamplePlayer,
        IUserTypingPreferenceService typingPreference)
    {
        _charMapper = charMapper;

        Session.LineChanged += (int lineNumber) =>
        {
            LineChanged?.Invoke(lineNumber);
        };

        Session.HitKeyStatusChanged += Session_HitKeyStatusChanged;


        _typingThemeProvider = typingThemeProvider;
        _themeChangerService = themeChangerService;
        _soundSamplePlayer = soundSamplePlayer;
        _typingPreference = typingPreference;

        _okVolume = typingPreference.GetOkVolume();
        _errorVolume = typingPreference.GetErrorVolume();
        Session.BackReturnEnable = typingPreference.GetBackReturnEnable();
        Session.StopOnError = typingPreference.GetStopOnError();
    }

    /// <summary>
    /// A random "correct key" sound, never the same twice in a row so the variation is heard.
    /// </summary>
    private string NextOkSound()
    {
        int index;
        if (_lastOkSoundIndex < 0)
        {
            index = Random.Shared.Next(s_keyOkSounds.Length);
        }
        else
        {
            // draw among the other sounds: skip the last one
            index = Random.Shared.Next(s_keyOkSounds.Length - 1);
            if (index >= _lastOkSoundIndex)
                index++;
        }

        _lastOkSoundIndex = index;
        return s_keyOkSounds[index];
    }

    private void Session_HitKeyStatusChanged(HitKeyStatus hitKeyStatus)
    {
        if (hitKeyStatus == HitKeyStatus.Success)
        {
            _soundSamplePlayer.PlaySound(NextOkSound(), OkVolume);
        }
        else
        {
            _soundSamplePlayer.PlaySound(KeyErrorSound, ErrorVolume);
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
        => _soundSamplePlayer.PlaySound(NextOkSound(), OkVolume);

    [RelayCommand]
    public void PreviewErrorSound()
        => _soundSamplePlayer.PlaySound(KeyErrorSound, ErrorVolume);

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

        await _soundSamplePlayer.PreloadAsync([.. s_keyOkSounds, KeyErrorSound]);

        // Get current theme apply
        ThemeState themeState = _themeChangerService.GetTheme();

        // TODO
        // to property, + user access
        Result <ITypingTheme> themeResu =
            await _typingThemeProvider.GetThemeAsync("XyloType_Typing_Theme", themeState);

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

        Session.ResetProgression();

        return Result<bool>.Ok(true);
    }

    public TypingStatus ProcessInput(char input)
        => Session.ProcessInput(input, _charMapper.Map);

    public void PauseTyping()
        => Session.Pause();

    public void ResumeTyping()
        => Session.Resume();


    public TypingSessionResult GetResult()
        => Session.GetResult();
}
