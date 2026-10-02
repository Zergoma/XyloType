using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Typing.Analysis;
using XyloType.Factories;
using XyloType.Navigation;

namespace XyloType.Services;

/// <summary>
/// Navigation asked by the view models: the views are shown in the main page by the <see cref="AppNavigator"/>.
/// </summary>
public class MauiNavigationService : INavigationService
{
    private readonly ITypingViewFactory _typingViewFactory;
    private readonly IStatisticViewFactory _statisticViewFactory;
    private readonly AppNavigator _navigator;

    public MauiNavigationService(
        ITypingViewFactory typingViewFactory,
        IStatisticViewFactory statisticViewFactory,
        AppNavigator navigator)
    {
        _typingViewFactory = typingViewFactory;
        _statisticViewFactory = statisticViewFactory;
        _navigator = navigator;
    }

    public async Task<Result<bool>> NavigateToTypingExerciseAsync(IStringsProvider stringProvider)
        => await ShowAsync(() => _typingViewFactory.CreateTypingViewAsync(stringProvider, this), _navigator.ShowExercise);

    public async Task<Result<bool>> ReplaceWithTypingExerciseAsync(IStringsProvider stringProvider)
        => await ShowAsync(() => _typingViewFactory.CreateTypingViewAsync(stringProvider, this), _navigator.ShowExercise);

    public async Task<Result<bool>> NavigateToStatisticAsync(TypingSessionResult result)
        => await ShowAsync(() => _statisticViewFactory.Create(result, this), _navigator.ShowExercise);

    public async Task<Result<bool>> ReplaceWithStatisticAsync(TypingSessionResult result)
        => await ShowAsync(() => _statisticViewFactory.Create(result, this), _navigator.ShowExercise);

    public Task PopBackAsync()
    {
        _navigator.CloseExercise();
        return Task.CompletedTask;
    }

    public Task PopToRootAsync()
    {
        _navigator.CloseExercise();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creating the typing or results view takes a while: the loading veil is shown meanwhile.
    /// </summary>
    private Task<Result<bool>> ShowAsync(Func<Task<Result<ContentView>>> create, Action<View> show)
        => _navigator.RunBusyAsync(async () =>
        {
            Result<ContentView> view = await create();
            if (!view.Success)
                return Result<bool>.Fail(view.Error);

            show(view.GetValue);
            return Result<bool>.Ok(true);
        });
}
