using Microsoft.Extensions.Logging;

using XyloType.MVVM.ViewModels;
using XyloType.MVVM.Views;
using XyloType.ViewModels.Statistic;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Factories;

public class StatisticViewFactory : IStatisticViewFactory
{
    private readonly IThemeChangerService _themeChangerService;
    private readonly IChartResponseTimeColorsProvider _chartResponseTimeColorsProvider;
    private readonly IChartErrorProvider _chartErrorColorsProvider;
    private readonly ILogger<StatisticViewModelMauiAdapter> _logger;
    private readonly ITypingExerciseRunService _runService;
    private readonly IUserTypingPreferenceService _typingPreference;
    private readonly IUserDialogService _dialogService;

    public StatisticViewFactory(
        IThemeChangerService themeChangerService,
        IChartResponseTimeColorsProvider chartResponseTimeColorsProvider,
        IChartErrorProvider chartErrorColorsProvider,
        ILogger<StatisticViewModelMauiAdapter> logger,
        ITypingExerciseRunService runService,
        IUserTypingPreferenceService typingPreference,
        IUserDialogService dialogService)
    {
        _themeChangerService = themeChangerService;
        _chartResponseTimeColorsProvider = chartResponseTimeColorsProvider;
        _chartErrorColorsProvider = chartErrorColorsProvider;
        _logger = logger;
        _runService = runService;
        _typingPreference = typingPreference;
        _dialogService = dialogService;
    }

    public async Task<Result<ContentView>> Create(
        TypingSessionResult result,
        INavigationService navigationService)
    {
        // Get current theme apply
        ThemeState themeState = _themeChangerService.GetTheme();


        StatisticViewModel vm =
            new(
                result,
                _runService,
                navigationService,
                _typingPreference,
                _dialogService);

        StatisticViewModelMauiAdapter vmadapter =
            new(
                vm,
                _chartResponseTimeColorsProvider,
                _chartErrorColorsProvider,
                themeState,
                _logger);
        
        vmadapter.Init();

        StatisticView vieww = new(vmadapter);

        return Result<ContentView>.Ok(vieww);
    }
}
