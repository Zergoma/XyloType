using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.MVVM.Views;
using XyloType.ViewModels;
using XyloType.ViewModels.Typing;

namespace XyloType.Factories;

public class TypingViewFactory : ITypingViewFactory
{
    private readonly ExerciseViewModelFactory _viewModels;

    public TypingViewFactory(ExerciseViewModelFactory viewModels)
    {
        _viewModels = viewModels;
    }

    public async Task<Result<ContentView>> CreateTypingViewAsync(
        IStringsProvider stringProvider,
        INavigationService navigationService)
    {
        Result<TypingViewModel> typing = await _viewModels.CreateTypingAsync(stringProvider);
        return typing.Success
            ? Result<ContentView>.Ok(new TypingView(typing.GetValue, navigationService))
            : Result<ContentView>.Fail(typing.Error);
    }
}
