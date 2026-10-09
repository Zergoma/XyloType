using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Typing.Analysis;
using XyloType.MVVM.Views;
using XyloType.ViewModels;

namespace XyloType.Factories;

public class StatisticViewFactory : IStatisticViewFactory
{
    private readonly ExerciseViewModelFactory _viewModels;

    public StatisticViewFactory(ExerciseViewModelFactory viewModels)
    {
        _viewModels = viewModels;
    }

    public async Task<Result<ContentView>> Create(
        TypingSessionResult result,
        INavigationService navigationService)
        => Result<ContentView>.Ok(new StatisticView(await _viewModels.CreateResultsAsync(result, navigationService)));
}
