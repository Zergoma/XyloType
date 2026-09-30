using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Typing.Analysis;
using XyloType.Factories;

namespace XyloType.Services;

public class MauiNavigationService : INavigationService
{
    private readonly ITypingViewFactory _typingViewFactory;
    private readonly IExerciceGeneratorViewFactory _exerciceViewFactory;
    private readonly IStatisticViewFactory _statisticViewFactory;

    public MauiNavigationService(
        ITypingViewFactory typingViewFactory,
        IExerciceGeneratorViewFactory exerciceViewFactory,
        IStatisticViewFactory statisticViewFactory)
    {
        _typingViewFactory = typingViewFactory;
        _exerciceViewFactory = exerciceViewFactory;
        _statisticViewFactory = statisticViewFactory;
    }

    public async Task<Result<bool>> NavigateToExerciceGeneratorAsync()
    {
        var exerciceGeneratorViewResult = 
            await _exerciceViewFactory.CreateExerciceGeneratorView();

        if (!exerciceGeneratorViewResult.Success)
        {
            return Result<bool>.Fail(exerciceGeneratorViewResult.Error);
        }

        await Shell.Current.Navigation.PushAsync(exerciceGeneratorViewResult.GetValue);

        return Result<bool>.Ok(true);
    }


    public async Task<Result<bool>> NavigateToUpdateExerciceAsync(Guid exerciceGuid)
    {
        var exerciceGeneratorViewResult =
            await _exerciceViewFactory.CreateExerciceUpdaterView(exerciceGuid);

        if (!exerciceGeneratorViewResult.Success)
        {
            return Result<bool>.Fail(exerciceGeneratorViewResult.Error);
        }

        await Shell.Current.Navigation.PushAsync(exerciceGeneratorViewResult.GetValue);

        return Result<bool>.Ok(true);
    }




    public async Task<Result<bool>> NavigateToTypingExerciseAsync(IStringsProvider stringProvider)
    {
        Result<ContentPage> typingviewResult =
            await _typingViewFactory.CreateTypingViewAsync(stringProvider, this);
        if(!typingviewResult.Success)
        {
            return Result<bool>.Fail(typingviewResult.Error);
        }

        await Shell.Current.Navigation.PushAsync(typingviewResult.GetValue);

        return Result<bool>.Ok(true);
    }


    public async Task<Result<bool>> NavigateToStatisticAsync(TypingSessionResult result)
    {
        Result<ContentPage> viewCReationResult = await _statisticViewFactory.Create(result, this);

        if(!viewCReationResult.Success)
        {
            return Result<bool>
                .Fail(viewCReationResult.Error);
        }

        await Shell.Current.Navigation.PushAsync(viewCReationResult.GetValue);

        return Result<bool>.Ok(true);
    }


    public async Task PopBackAsync()
    {
        await Shell.Current.Navigation.PopAsync();
    }

    public async Task<Result<bool>> ReplaceWithTypingExerciseAsync(IStringsProvider stringProvider)
        => await ReplaceCurrentPageAsync(() => NavigateToTypingExerciseAsync(stringProvider));

    public async Task<Result<bool>> ReplaceWithStatisticAsync(TypingSessionResult result)
        => await ReplaceCurrentPageAsync(() => NavigateToStatisticAsync(result));

    public async Task PopToRootAsync()
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }

    /// <summary>
    /// Pushes the new page first, then removes the previous one,
    /// so the home screen never flashes in between.
    /// </summary>
    private static async Task<Result<bool>> ReplaceCurrentPageAsync(Func<Task<Result<bool>>> navigate)
    {
        INavigation navigation = Shell.Current.Navigation;
        Page? current = navigation.NavigationStack.LastOrDefault();

        Result<bool> result = await navigate();

        if (result.Success && current is not null && navigation.NavigationStack.Contains(current))
        {
            navigation.RemovePage(current);
        }

        return result;
    }
}
