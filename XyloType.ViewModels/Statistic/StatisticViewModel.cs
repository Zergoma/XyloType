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
