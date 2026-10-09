using Microsoft.Extensions.Logging;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Typing.Analysis;
using XyloType.ViewModels.Statistic;
using XyloType.ViewModels.Typing;

namespace XyloType.ViewModels;

/// <summary>
/// The view models of an exercise being played: the typing screen with its text loaded,
/// then its results (kept for the current user before they are shown). Each app wraps them in its views.
/// </summary>
public class ExerciseViewModelFactory
{
    private readonly ITypingThemeProvider _typingThemeProvider;
    private readonly IInputCharMapperService _charMapper;
    private readonly IThemeChangerService _themeChangerService;
    private readonly IPlaySoundSample _soundPlayer;
    private readonly IUserTypingPreferenceService _typingPreference;
    private readonly IScoreCatalog _scoreCatalog;
    private readonly ITypingExerciseRunService _runService;
    private readonly IUserDialogService _dialogService;
    private readonly IExerciseProgressService _progress;
    private readonly IStatColorScale _colors;
    private readonly ILogger<StatisticChartsViewModel> _logger;

    public ExerciseViewModelFactory(
        ITypingThemeProvider typingThemeProvider,
        IInputCharMapperService charMapper,
        IThemeChangerService themeChangerService,
        IPlaySoundSample soundPlayer,
        IUserTypingPreferenceService typingPreference,
        IScoreCatalog scoreCatalog,
        ITypingExerciseRunService runService,
        IUserDialogService dialogService,
        IExerciseProgressService progress,
        IStatColorScale colors,
        ILogger<StatisticChartsViewModel> logger)
    {
        _typingThemeProvider = typingThemeProvider;
        _charMapper = charMapper;
        _themeChangerService = themeChangerService;
        _soundPlayer = soundPlayer;
        _typingPreference = typingPreference;
        _scoreCatalog = scoreCatalog;
        _runService = runService;
        _dialogService = dialogService;
        _progress = progress;
        _colors = colors;
        _logger = logger;
    }

    /// <summary>
    /// The typing screen, its text created by <paramref name="stringProvider"/>.
    /// </summary>
    public async Task<Result<TypingViewModel>> CreateTypingAsync(IStringsProvider stringProvider)
    {
        TypingViewModel typing = new(
            _charMapper,
            _typingThemeProvider,
            _themeChangerService,
            _soundPlayer,
            _typingPreference,
            _scoreCatalog);

        Result<bool> loaded = await typing.LoadTextAsync(stringProvider);
        return loaded.Success
            ? Result<TypingViewModel>.Ok(typing)
            : Result<TypingViewModel>.Fail(loaded.Error);
    }

    /// <summary>
    /// The results of a session, in the colors of the theme shown; the result is kept for the current user first.
    /// </summary>
    public async Task<StatisticChartsViewModel> CreateResultsAsync(TypingSessionResult result, INavigationService navigation)
    {
        StatisticViewModel results = new(result, _runService, navigation, _typingPreference, _dialogService, _progress);
        await results.RecordAsync();

        StatisticChartsViewModel charts = new(results, _colors, _themeChangerService.GetTheme(), _logger);
        charts.Init();
        return charts;
    }
}
