using Microsoft.Extensions.Logging;

namespace XyloType.Navigation;

/// <summary>
/// Navigation of the single main page: the view shown in its host, like a WPF ContentControl.
/// The main sections show one view each, created once and kept.
/// The exercise is a section of its own, open while an exercise or its results are shown;
/// going to another section closes it.
/// </summary>
public class AppNavigator
{
    private static readonly Dictionary<AppSection, Type> s_sectionViews = new()
    {
        [AppSection.Home] = typeof(MVVM.Views.TypingLauncherView),
        [AppSection.Exercises] = typeof(MVVM.Views.ExercisesManagerView),
        [AppSection.Words] = typeof(MVVM.Views.WordsExplorerView),
        [AppSection.Import] = typeof(MVVM.Views.ImportView),
    };

    private readonly IServiceProvider _services;
    private readonly ILogger<AppNavigator> _logger;
    private readonly Dictionary<AppSection, View> _sectionViews = [];

    // the exercise, then its results (shown in place of the exercise)
    private View? _exerciseView;

    public AppNavigator(IServiceProvider services, ILogger<AppNavigator> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// The view to show changed: the host shows it.
    /// </summary>
    public event Action<View>? CurrentViewChanged;

    /// <summary>
    /// The section shown, or the exercise view, changed (the rail updates its buttons).
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// A view is being created or loaded: the main page shows a loading veil (true), or hides it (false).
    /// </summary>
    public event Action<bool>? BusyChanged;

    public bool IsBusy { get; private set; }

    public AppSection CurrentSection { get; private set; } = AppSection.Home;

    public View? CurrentView { get; private set; }

    /// <summary>
    /// The open exercise or its results, if any.
    /// </summary>
    public View? ExerciseView => _exerciseView;

    private View SectionView(AppSection section)
    {
        if (section == AppSection.Exercise)
            return _exerciseView ?? SectionView(AppSection.Home);

        if (!_sectionViews.TryGetValue(section, out View? view))
        {
            view = (View)_services.GetRequiredService(s_sectionViews[section]);
            _sectionViews[section] = view;
        }

        return view;
    }

    /// <summary>
    /// The views kept in the host: the section views, and the open exercise.
    /// </summary>
    public bool IsKept(View view)
        => view == _exerciseView || _sectionViews.ContainsValue(view);

    /// <summary>
    /// Shows the first section, at start.
    /// </summary>
    public void Start()
        => Show(SectionView(CurrentSection));

    /// <summary>
    /// Goes to a section of the bottom of the rail: the open exercise (or its results) is closed.
    /// </summary>
    public async Task GoToSectionAsync(AppSection section)
    {
        if (IsBusy)
            return;

        if (section == CurrentSection || section == AppSection.Exercise)
            return;

        if (CurrentView is INavigationGuard guard && !await guard.CanLeaveAsync())
            return;

        try
        {
            // first visit: the view is created, which takes a while
            if (!_sectionViews.ContainsKey(section))
            {
                await RunBusyAsync(() =>
                {
                    ShowSection(section);
                    return Task.CompletedTask;
                });
                return;
            }

            ShowSection(section);
        }
        catch (Exception ex)
        {
            // the section stays the previous one: a click on its button tries again
            _logger.LogError(ex, "Unable to show the section {Section}", section);
        }
    }

    private void ShowSection(AppSection section)
    {
        // created before anything changes: if it fails, the app stays where it was
        View view = SectionView(section);

        // leaving the exercise ends it: there is no going back to it
        _exerciseView = null;
        CurrentSection = section;
        Show(view);
    }

    /// <summary>
    /// Opens an exercise, in place of the one open before, or shows its results in place of it.
    /// </summary>
    public void ShowExercise(View view)
    {
        _exerciseView = view;
        CurrentSection = AppSection.Exercise;
        Show(view);
    }

    /// <summary>
    /// Closes the exercise and goes back home.
    /// </summary>
    public void CloseExercise()
    {
        _exerciseView = null;
        CurrentSection = AppSection.Home;
        Show(SectionView(AppSection.Home));
    }

    /// <summary>
    /// Shows the loading veil while a long work runs on the UI thread (creating a view):
    /// the veil is given the time to appear first, and to be removed once the new view is laid out.
    /// </summary>
    public async Task<T> RunBusyAsync<T>(Func<Task<T>> work)
    {
        IsBusy = true;
        BusyChanged?.Invoke(true);
        try
        {
            await Task.Delay(s_veilDelay);
            T result = await work();
            await Task.Delay(s_veilDelay);
            return result;
        }
        finally
        {
            IsBusy = false;
            BusyChanged?.Invoke(false);
        }
    }

    public Task RunBusyAsync(Func<Task> work)
        => RunBusyAsync(async () => { await work(); return true; });

    // time for the veil to be drawn before the UI thread gets busy
    private static readonly TimeSpan s_veilDelay = TimeSpan.FromMilliseconds(40);

    private void Show(View view)
    {
        if (view != CurrentView)
        {
            (CurrentView as IViewLifecycle)?.OnDisappearing();

            CurrentView = view;
            CurrentViewChanged?.Invoke(view);

            (view as IViewLifecycle)?.OnAppearing();
        }

        StateChanged?.Invoke();
    }
}
