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
    public event Action<int>? LineChanged;

    public TypingSession Session { get; } = new();
    public ObservableCollection<TypingLineStateViewModel> LinesStates { get; } = [];

    private readonly ITypingThemeProvider _typingThemeProvider;
    private readonly IInputCharMapperService _charMapper;
    private readonly IThemeChangerService _themeChangerService;
    private readonly IPlaySoundSample _soundSamplePlayer;


    public TypingViewModel(
        IInputCharMapperService charMapper,
        ITypingThemeProvider typingThemeProvider,
        IThemeChangerService themeChangerService,
        IPlaySoundSample soundSamplePlayer)
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
    }

    private async void Session_HitKeyStatusChanged(HitKeyStatus hitKeyStatus)
    {
        if (hitKeyStatus == HitKeyStatus.Success)
        {
            await _soundSamplePlayer.PLaySoundAsync("keypress.wav", .3);
        }
        else
        {
            await _soundSamplePlayer.PLaySoundAsync("Mi.wav", .5);
        }
    }

    public bool StopOnErrorEnable
    {
        get => Session.StopOnError;
        set
        {
            if (Session.StopOnError == value)
                return;
            Session.StopOnError = value;
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

        // Get current theme apply
        ThemeState themeState = _themeChangerService.GetTheme();

        // TODO
        // to property, + user access
        Result <ITypingTheme> themeResu =
            await _typingThemeProvider.GetThemeAsync("OctoType_Typing_Theme", themeState);

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
