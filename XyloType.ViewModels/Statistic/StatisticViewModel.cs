using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.ViewModels.Statistic;

public partial class StatisticViewModel : ObservableObject
{
    private readonly ITypingExerciseRunService _runService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IUserTypingPreferenceService _typingPreference;

    [ObservableProperty]
    public partial Dictionary<char, CharStats> Statistics { get; set; }

    /// <summary>
    /// Time of the session, from the first key press to the last character, pauses excluded.
    /// </summary>
    public TimeSpan Duration { get; }

    public StatisticViewModel(
        TypingSessionResult result,
        ITypingExerciseRunService runService,
        INavigationService navigationService,
        IUserTypingPreferenceService typingPreference,
        IUserDialogService dialogService)
    {
        Statistics = result.CharStats;
        Duration = result.Duration;
        _runService = runService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _typingPreference = typingPreference;

        ShowSpeed = typingPreference.GetShowSpeedResult();
        ShowResponseTime = typingPreference.GetShowResponseTimeResult();
        ShowErrors = typingPreference.GetShowErrorsResult();
        GroupResponseTimes = typingPreference.GetGroupResponseTimes();
        GroupErrors = typingPreference.GetGroupErrors();
    }

    public string ExerciseName
        => _runService.CurrentExerciseName;

    public bool HasExerciseName
        => !string.IsNullOrWhiteSpace(ExerciseName);

    #region Sections: each one can be folded (remembered, same settings as on the typing page)

    [ObservableProperty]
    public partial bool ShowSpeed { get; set; }

    [ObservableProperty]
    public partial bool ShowResponseTime { get; set; }

    [ObservableProperty]
    public partial bool ShowErrors { get; set; }

    partial void OnShowSpeedChanged(bool value) => _typingPreference.SetShowSpeedResult(value);
    partial void OnShowResponseTimeChanged(bool value) => _typingPreference.SetShowResponseTimeResult(value);
    partial void OnShowErrorsChanged(bool value) => _typingPreference.SetShowErrorsResult(value);

    [RelayCommand]
    public void ToggleSpeed() => ShowSpeed = !ShowSpeed;

    [RelayCommand]
    public void ToggleResponseTime() => ShowResponseTime = !ShowResponseTime;

    [RelayCommand]
    public void ToggleErrors() => ShowErrors = !ShowErrors;

    #endregion

    #region Grouping of the keys with close values (remembered)

    [ObservableProperty]
    public partial bool GroupResponseTimes { get; set; }

    [ObservableProperty]
    public partial bool GroupErrors { get; set; }

    partial void OnGroupResponseTimesChanged(bool value) => _typingPreference.SetGroupResponseTimes(value);
    partial void OnGroupErrorsChanged(bool value) => _typingPreference.SetGroupErrors(value);

    #endregion

    public bool HasNext
        => _runService.HasNext;

    [RelayCommand]
    public async Task Retry()
    {
        await StartAsync(_runService.Retry());
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    public async Task Next()
    {
        await StartAsync(_runService.Next());
    }

    private async Task StartAsync(Result<IStringsProvider> providerResult)
    {
        Result<bool> navigationResult =
            providerResult.Success
                ? await _navigationService.ReplaceWithTypingExerciseAsync(providerResult.GetValue)
                : Result<bool>.Fail(providerResult.Error);

        if (!navigationResult.Success)
            await _dialogService.AlertAsync("Impossible de lancer l'exercice", navigationResult.Error);
    }

    [RelayCommand]
    public async Task Home()
        => await _navigationService.PopToRootAsync();
}
