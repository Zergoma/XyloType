using Avalonia.Controls;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Desktop.Views;
using XyloType.Domain.Typing.Analysis;
using XyloType.ViewModels;
using XyloType.ViewModels.Typing;

namespace XyloType.Desktop.Services;

/// <summary>
/// The views of an exercise being played, around the view models of <see cref="ExerciseViewModelFactory"/>.
/// </summary>
public class ExerciseViewFactory
{
    private readonly ExerciseViewModelFactory _viewModels;

    public ExerciseViewFactory(ExerciseViewModelFactory viewModels)
    {
        _viewModels = viewModels;
    }

    public async Task<Result<Control>> CreateTypingAsync(IStringsProvider stringProvider, INavigationService navigation)
    {
        Result<TypingViewModel> typing = await _viewModels.CreateTypingAsync(stringProvider);
        return typing.Success
            ? Result<Control>.Ok(new TypingView(typing.GetValue, navigation))
            : Result<Control>.Fail(typing.Error);
    }

    public async Task<Result<Control>> CreateResultsAsync(TypingSessionResult result, INavigationService navigation)
        => Result<Control>.Ok(new StatisticView(await _viewModels.CreateResultsAsync(result, navigation)));
}
