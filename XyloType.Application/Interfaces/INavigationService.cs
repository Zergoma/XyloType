using XyloType.Domain.Typing.Analysis;

namespace XyloType.Application.Interfaces;

public interface INavigationService
{
    Task<Result<bool>> NavigateToTypingExerciseAsync(IStringsProvider stringProvider);

    /// <summary>
    /// Opens the typing screen in place of the current page (e.g. from the results screen).
    /// </summary>
    Task<Result<bool>> ReplaceWithTypingExerciseAsync(IStringsProvider stringProvider);

    Task<Result<bool>> NavigateToStatisticAsync(TypingSessionResult result);

    /// <summary>
    /// Opens the results screen in place of the current page (the typing screen).
    /// </summary>
    Task<Result<bool>> ReplaceWithStatisticAsync(TypingSessionResult result);

    Task PopBackAsync();

    /// <summary>
    /// Goes back to the home screen (exercise launcher).
    /// </summary>
    Task PopToRootAsync();
}
