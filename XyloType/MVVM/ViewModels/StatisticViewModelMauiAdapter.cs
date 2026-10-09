using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.Logging;

using XyloType.ViewModels.Statistic;

using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.MVVM.ViewModels;


public partial class StatisticViewModelMauiAdapter : ObservableObject
{
    private readonly StatisticViewModel _statisticViewModel;

    private readonly IChartResponseTimeColorsProvider _chartResponseTimeColorsProvider;
    private readonly IChartErrorProvider _chartErrorColorsProvider;
    private readonly ThemeState _themeState;
    private readonly ILogger<StatisticViewModelMauiAdapter> _logger;

    public StatisticViewModelMauiAdapter(
        StatisticViewModel statisticViewModel,
        IChartResponseTimeColorsProvider chartResponseTimeColorsProvider,
        IChartErrorProvider chartErrorColorsProvider,
        ThemeState themeState,
        ILogger<StatisticViewModelMauiAdapter> logger)
    {
        _statisticViewModel = statisticViewModel;
        _chartResponseTimeColorsProvider = chartResponseTimeColorsProvider;
        _themeState = themeState;
        _chartErrorColorsProvider = chartErrorColorsProvider;
        _logger = logger;
    }

    // the figures of the session come from the domain
    private TypingSessionResult Result => _statisticViewModel.Result;

    // Duration of the session, without the time spent out of the typing area
    public double TotalMinute => Result.Duration.TotalMinutes;

    public double LettersPerMinute => Result.CharactersPerMinute;

    public double WordsPerMinute => Result.WordsPerMinute;

    public string LetterPerMinuteText => $"{LettersPerMinute:F0}";
    public string WordsPerMinuteText => $"{WordsPerMinute:F1}";

    /// <summary>
    /// Result actions (retry, next, home) and display preferences.
    /// </summary>
    public StatisticViewModel Core => _statisticViewModel;

    /// <summary>
    /// Number of wrong key presses (a character can be missed several times).
    /// </summary>
    public int TotalErrors => Result.WrongKeyPresses;

    // Share of characters typed right the first time, in percent
    public double Accuracy => 100.0 * Result.Accuracy;

    public string AccuracyText => $"{Accuracy:F0} %";
    public string TotalErrorsText => TotalErrors.ToString();

    public string DurationText
    {
        get
        {
            TimeSpan duration = TimeSpan.FromMinutes(TotalMinute);
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"m\:ss");
        }
    }

    public bool ShowErrorsChart => HasError;
    public bool ShowNoErrorMessage => !HasError;

    [ObservableProperty]
    public partial bool HasError { get; set; } = false;

    // keys whose values differ by less than this are grouped, when the grouping is on
    private const double ResponseTimeTolerance = 0.1;
    private const double ErrorRateTolerance = 5;

    // a pause would make every other bar useless: 5 s at most per key
    private const double MaxResponseTime = 5;

    private readonly List<KeyValue> _responseTimes = [];
    private readonly List<KeyValue> _errorRates = [];
    private readonly Dictionary<char, CharStats> _errorStats = [];

    /// <summary>
    /// Average response time per key, slowest first.
    /// </summary>
    public ObservableCollection<StatBarItem> ResponseTimeBars { get; } = [];

    /// <summary>
    /// Share of the occurrences of each key typed wrong at least once, worst first.
    /// </summary>
    public ObservableCollection<StatBarItem> ErrorBars { get; } = [];

    public void Init()
    {
        foreach (KeyValuePair<char, CharStats> item in _statisticViewModel.Statistics)
        {
            CharStats charStats = item.Value;

            _responseTimes.Add(new KeyValue(item.Key, Math.Min(charStats.ResponseTimeAverage.TotalSeconds, MaxResponseTime)));

            if (charStats.NbCharError > 0)
            {
                double errorPercentage = charStats.NbOccurence > 0
                    ? charStats.NbCharError * 100.0 / charStats.NbOccurence
                    : 100.0;

                _errorRates.Add(new KeyValue(item.Key, errorPercentage));
                _errorStats[item.Key] = charStats;
            }
        }

        HasError = _errorRates.Count > 0;

        BuildResponseTimeBars();
        BuildErrorBars();

        // the grouping switches rebuild their chart
        Core.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StatisticViewModel.GroupResponseTimes))
                BuildResponseTimeBars();
            else if (e.PropertyName == nameof(StatisticViewModel.GroupErrors))
                BuildErrorBars();
        };

        _logger.LogInformation(
            "Letters per minute {LPM}, Words per minute {WPM}, Errors {CharsError}",
            LettersPerMinute,
            WordsPerMinute,
            _statisticViewModel.Statistics
            .Where(x => x.Value.RealErrors.Count  >0 )
            .Select(x => (
                Character :x.Key, 
                Count : x.Value.RealErrors.Count,
                Errors : string.Join(null, x.Value.RealErrors)
            )));
    }

    private void BuildResponseTimeBars()
    {
        IReadOnlyList<KeyGroup> groups = StatBars.Group(_responseTimes, ResponseTimeTolerance, Core.GroupResponseTimes);
        double max = groups.Count > 0 ? Math.Max(groups.Max(g => g.Value), 0.01) : 1;

        ResponseTimeBars.Clear();
        foreach (KeyGroup group in groups)
        {
            ResponseTimeBars.Add(new StatBarItem(
                StatBars.Label(group.Keys),
                group.Value,
                group.IsSingle ? $"{group.Value:0.00} s" : $"≈ {group.Value:0.00} s",
                group.Value / max,
                _chartResponseTimeColorsProvider.GetHexColorTimeResponse(group.Value, _themeState)));
        }
    }

    private void BuildErrorBars()
    {
        IReadOnlyList<KeyGroup> groups = StatBars.Group(_errorRates, ErrorRateTolerance, Core.GroupErrors);
        double max = groups.Count > 0 ? Math.Max(groups.Max(g => g.Value), 1) : 1;

        ErrorBars.Clear();
        foreach (KeyGroup group in groups)
        {
            string valueText;
            if (group.IsSingle)
            {
                CharStats stats = _errorStats[group.Keys[0].Key];
                valueText = $"{stats.NbCharError}/{stats.NbOccurence} ({group.Value:0} %)";
            }
            else
            {
                valueText = $"≈ {group.Value:0} %";
            }

            ErrorBars.Add(new StatBarItem(
                StatBars.Label(group.Keys),
                group.Value,
                valueText,
                group.Value / max,
                _chartErrorColorsProvider.GetHexColorError(group.Value, _themeState)));
        }
    }
}
