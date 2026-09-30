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
        IUserTypingPreferenceService typingPreference)
    {
        Statistics = result.CharStats;
        Duration = result.Duration;
        _runService = runService;
        _navigationService = navigationService;

        ShowSpeed = typingPreference.GetShowSpeedResult();
        ShowResponseTime = typingPreference.GetShowResponseTimeResult();
        ShowErrors = typingPreference.GetShowErrorsResult();
    }

    public string ExerciseName
        => _runService.CurrentExerciseName;

    public bool HasExerciseName
        => !string.IsNullOrWhiteSpace(ExerciseName);

    public bool ShowSpeed { get; }

    public bool ShowResponseTime { get; }

    public bool ShowErrors { get; }

    public bool IsEverythingHidden
        => !ShowSpeed && !ShowResponseTime && !ShowErrors;

    public bool HasNext
        => _runService.HasNext;

    [RelayCommand]
    public async Task Retry()
    {
        Result<IStringsProvider> retryResult = _runService.Retry();
        if (!retryResult.Success)
            return;

        await _navigationService.ReplaceWithTypingExerciseAsync(retryResult.GetValue);
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    public async Task Next()
    {
        Result<IStringsProvider> nextResult = _runService.Next();
        if (!nextResult.Success)
            return;

        await _navigationService.ReplaceWithTypingExerciseAsync(nextResult.GetValue);
    }

    [RelayCommand]
    public async Task Home()
        => await _navigationService.PopToRootAsync();
}
