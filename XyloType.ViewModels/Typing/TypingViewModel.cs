using System.Collections.ObjectModel;
using System.Diagnostics;

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
    private const string KeyOkSound = "key_ok.wav";
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

    private void Session_HitKeyStatusChanged(HitKeyStatus hitKeyStatus)
    {
        if (hitKeyStatus == HitKeyStatus.Success)
        {
            _soundSamplePlayer.PlaySound(KeyOkSound, OkVolume);
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
        => _soundSamplePlayer.PlaySound(KeyOkSound, OkVolume);

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

    public async Task LoadTextAsync(IStringsProvider stringProvider)
    {
        Session.Lines.Clear();
        LinesStates.Clear();

        await _soundSamplePlayer.PreloadAsync(KeyOkSound, KeyErrorSound);

        // Get current theme apply
        ThemeState themeState = _themeChangerService.GetTheme();

        // TODO
        // to property, + user access
        Result <ITypingTheme> themeResu =
            await _typingThemeProvider.GetThemeAsync("XyloType_Typing_Theme", themeState);

        if (!themeResu.Success)
        {
            return;
        }

        Result<IEnumerable<string>> getStringResult =
            await stringProvider.GetStringsAsync();

        if (!getStringResult.Success)
        {
            Debug.WriteLine(getStringResult.Error);
            return;
        }

        string[] dataLines = [.. getStringResult.GetValue];

        ArgumentOutOfRangeException.ThrowIfLessThan(dataLines.Length, 1);

        foreach (string line in dataLines)
        {
            TypingLine typingLine = new(line);
            Session.Lines.Add(typingLine);

            TypingLineStateViewModel typingLineState = new(themeResu.GetValue, typingLine);
            LinesStates.Add(typingLineState);
        }

        Session.ResetProgression();
    }

    public TypingStatus ProcessInput(char input)
        => Session.ProcessInput(input, _charMapper.Map);


    public Dictionary<char, CharStats> GetTotalCharStats()
    {
        Dictionary<char, CharStats> total = [];

        foreach (TypingLineStateViewModel itemLine in LinesStates)
        {
            Dictionary<char, CharStats> stat = itemLine.GetLineCharStats();
            
            foreach (KeyValuePair<char, CharStats> item in stat)
            {
                if (total.TryGetValue(item.Key, out CharStats? charstat))
                {
                    total[item.Key] = charstat.Add(item.Value);
                }
                else
                {
                    total[item.Key] = item.Value;
                }
            }
        }
        return total;
    }


}
