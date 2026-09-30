using XyloType.MVVM.Views;

namespace XyloType.DI;

public static class ViewsModule
{
    public static IServiceCollection AddMauiViews(this IServiceCollection services)
    {
        services.AddTransient<ImportWordView>();
        services.AddTransient<ImportBookView>();
        services.AddTransient<TypingLauncherView>();
        services.AddTransient<ExercisesManagerView>();

        return services;
    }
}
