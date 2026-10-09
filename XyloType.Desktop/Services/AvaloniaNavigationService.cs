using Avalonia.Controls;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Desktop.Navigation;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Desktop.Services;

/// <summary>
/// Navigation asked by the view models: the views are shown in the main window by the <see cref="AppNavigator"/>.
/// </summary>
public class AvaloniaNavigationService : INavigationService
{
    private readonly ExerciseViewFactory _views;
    private readonly AppNavigator _navigator;

    public AvaloniaNavigationService(ExerciseViewFactory views, AppNavigator navigator)
    {
        _views = views;
        _navigator = navigator;
    }

    public Task<Result<bool>> NavigateToTypingExerciseAsync(IStringsProvider stringProvider)
        => ShowAsync(() => _views.CreateTypingAsync(stringProvider, this));

    public Task<Result<bool>> ReplaceWithTypingExerciseAsync(IStringsProvider stringProvider)
        => ShowAsync(() => _views.CreateTypingAsync(stringProvider, this));

    public Task<Result<bool>> NavigateToStatisticAsync(TypingSessionResult result)
        => ShowAsync(() => _views.CreateResultsAsync(result, this));

    public Task<Result<bool>> ReplaceWithStatisticAsync(TypingSessionResult result)
        => ShowAsync(() => _views.CreateResultsAsync(result, this));

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
    private Task<Result<bool>> ShowAsync(Func<Task<Result<Control>>> create)
        => _navigator.RunBusyAsync(async () =>
        {
            Result<Control> view = await create();
            if (!view.Success)
                return Result<bool>.Fail(view.Error);

            _navigator.ShowExercise(view.GetValue);
            return Result<bool>.Ok(true);
        });
}
