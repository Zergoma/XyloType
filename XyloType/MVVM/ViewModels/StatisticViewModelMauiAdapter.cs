using CommunityToolkit.Mvvm.ComponentModel;

using Microcharts;

using Microsoft.Extensions.Logging;

using XyloType.ViewModels.Statistic;

using SkiaSharp;

using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.MVVM.ViewModels;


public partial class StatisticViewModelMauiAdapter : ObservableObject
{
    private readonly StatisticViewModel _statisticViewModel;

    [ObservableProperty]
    public partial BarChart TimeResponseChart { get; set; }

    [ObservableProperty]
    public partial RadarChart ErrorsChart { get; set; }

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

    public int TotalOccurence { get; set; } = 0;
    // Duration of the session, without the time spent out of the typing area
    public double TotalMinute => _statisticViewModel.Duration.TotalMinutes;


    public double LettersPerMinute => TotalMinute > 0
                                        ? TotalOccurence / TotalMinute
                                        : 0.0;

    public double WordsPerMinute => LettersPerMinute / 5.0;

    public string LetterPerMinuteText => $"{LettersPerMinute:F0}";
    public string WordsPerMinuteText => $"{WordsPerMinute:F1}";

    /// <summary>
    /// Result actions (retry, next, home) and display preferences.
    /// </summary>
    public StatisticViewModel Core => _statisticViewModel;

    /// <summary>
    /// Number of wrong key presses (a character can be missed several times).
    /// </summary>
    public int TotalErrors { get; private set; }

    /// <summary>
    /// Number of characters that needed at least one retry.
    /// </summary>
    public int TotalCharsWithError { get; private set; }

    // Share of characters typed right the first time
    public double Accuracy => TotalOccurence > 0
                                ? 100.0 * (TotalOccurence - TotalCharsWithError) / TotalOccurence
                                : 100.0;

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

    public bool ShowSpeedSection => Core.ShowSpeed || Core.ShowResponseTime;
    public bool ShowErrorsChart => Core.ShowErrors && HasError;
    public bool ShowNoErrorMessage => Core.ShowErrors && !HasError;

    [ObservableProperty]
    public partial bool HasError { get; set; } = false;

    public void Init()
    {
        List<ChartEntry> gatherResponseTime = [];
        List<ChartEntry> gatherError = [];

        foreach (KeyValuePair<char, CharStats> item in _statisticViewModel.Statistics)
        {
            CharStats charStats = item.Value;

            TotalOccurence += charStats.NbOccurence;
            TotalErrors += charStats.RealErrors.Count;
            TotalCharsWithError += charStats.NbCharError;

            // We concidere 5sec as the maximum time to press the key
            // This to avoid to have chart useless because of a pause
            double timeResponseAverage = Math.Min(charStats.ResponseTimeAverage.TotalSeconds, 5.0);

            SKColor colorLabel = SKColor.Parse(_chartResponseTimeColorsProvider.GetHexColorTimeResponse(timeResponseAverage, _themeState));
            SKColor colorText = SKColor.Parse(_chartResponseTimeColorsProvider.GetHexColorTxtLabel(_themeState));

            var timeResponseEntry =
                new ChartEntry((float)timeResponseAverage)
                {
                    Label = item.Key.ToString(),
                    ValueLabel = $"{timeResponseAverage:f2}",
                    Color = colorLabel,
                    ValueLabelColor = colorText,
                    TextColor = colorText,
                };

            gatherResponseTime.Add(timeResponseEntry);


            if (charStats.NbCharError > 0)
            {
                double errorPercentage = 
                    charStats.NbOccurence > 0
                    ? charStats.NbCharError*100.0 / charStats.NbOccurence
                    : 100.0;

                SKColor colorError = SKColor.Parse(_chartErrorColorsProvider.GetHexColorError(errorPercentage, _themeState));

                var errorEntry =
                    new ChartEntry((float)errorPercentage)
                    {
                        Label = item.Key.ToString(),
                        ValueLabel = $"{charStats.NbCharError}/{charStats.NbOccurence} ({errorPercentage:f2})%",
                        Color = colorError,
                        ValueLabelColor = colorText,
                        TextColor = colorText,
                    };
                gatherError.Add(errorEntry);
            }

        }


        SKColor colorBg = SKColor.Parse(_chartResponseTimeColorsProvider.GetHexColorBg(_themeState));

        TimeResponseChart =
            new BarChart()
            {
                Entries = [.. gatherResponseTime.OrderByDescending(x => x.Value)],
                MinValue = 0,
                //MaxValue = 5,
                LabelOrientation = Orientation.Horizontal,
                BackgroundColor = colorBg,
                CornerRadius = 5,
            };

        HasError = gatherError.Count > 0;

        ErrorsChart =
            new RadarChart()
            {
                Entries = [.. gatherError.OrderByDescending(x => x.Value)],
                MinValue = 0,
                LabelTextSize = 10,
                BackgroundColor = colorBg,
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

}
