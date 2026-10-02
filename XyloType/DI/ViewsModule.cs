using XyloType.MVVM.Views;
using XyloType.Navigation;

namespace XyloType.DI;

public static class ViewsModule
{
    public static IServiceCollection AddMauiViews(this IServiceCollection services)
    {
        services.AddTransient<ImportWordView>();
        services.AddTransient<ImportBookView>();
        services.AddTransient<TypingLauncherView>();
        services.AddTransient<ExercisesManagerView>();
        services.AddTransient<WordsExplorerView>();
        services.AddTransient<ImportView>();

        // the single page and its navigation
        services.AddSingleton<AppNavigator>();
        services.AddSingleton<MainPage>();

        return services;
    }
}
