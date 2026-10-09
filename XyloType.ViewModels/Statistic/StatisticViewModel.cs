using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.ViewModels.Statistic;

public partial class StatisticViewModel : ObservableObject
{
    private readonly ITypingExerciseRunService _runService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IUserTypingPreferenceService _typingPreference;
    private readonly IExerciseProgressService _progress;

    [ObservableProperty]
    public partial Dictionary<char, CharStats> Statistics { get; set; }

    /// <summary>
    /// Time of the session, from the first key press to the last character, pauses excluded.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// The figures of the session.
    /// </summary>
    public TypingSessionResult Result { get; }

    /// <summary>
    /// What is good at the level of the exercise.
    /// </summary>
    public TypingTargets Targets { get; }

    /// <summary>
    /// E.g. "Repères du niveau Débutant".
    /// </summary>
    public string LevelText { get; }

    public StatisticViewModel(
        TypingSessionResult result,
        ITypingExerciseRunService runService,
        INavigationService navigationService,
        IUserTypingPreferenceService typingPreference,
        IUserDialogService dialogService,
        IExerciseProgressService progress)
    {
        Result = result;
        Targets = TypingTargets.For(runService.CurrentLevel);
        LevelText = $"Repères du niveau {TypingLevelNames.Of(runService.CurrentLevel)}";
        Statistics = result.CharStats;
        Duration = result.Duration;
        _runService = runService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _typingPreference = typingPreference;
        _progress = progress;

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

    #region Score: kept for the current user, compared with its best

    public string ScoreText => $"{Result.Score:N1}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScoreComment))]
    public partial string ScoreComment { get; set; } = string.Empty;

    public bool HasScoreComment => ScoreComment.Length > 0;

    [ObservableProperty]
    public partial bool IsRecord { get; set; }

    /// <summary>
    /// Keeps the result for the current user (once, when the results are shown).
    /// </summary>
    public async Task RecordAsync()
    {
        if (_runService.CurrentExerciseId is not Guid exerciseId)
            return;

        Result<ExerciseAttemptOutcome> outcome = await _progress.RecordAsync(exerciseId, Result);
        if (!outcome.Success)
            return;

        ExerciseAttemptOutcome attempt = outcome.GetValue;
        IsRecord = attempt.IsRecord;
        ScoreComment = attempt.PreviousBest switch
        {
            null => "Premier essai : c'est votre score de référence",
            double best when attempt.IsRecord => $"Nouveau record ! (ancien : {best:N1})",
            double best => $"Votre meilleur : {best:N1}",
        };
    }

    #endregion

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
