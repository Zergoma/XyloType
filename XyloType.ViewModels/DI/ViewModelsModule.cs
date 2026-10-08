using Microsoft.Extensions.DependencyInjection;

using XyloType.ViewModels.Theme;
using XyloType.ViewModels.ExercisesManager;
using XyloType.ViewModels.Import;
using XyloType.ViewModels.WordsExplorer;
using XyloType.ViewModels.TypingLauncher;


namespace XyloType.ViewModels.DI;

public static class ViewModelsModule
{
    public static IServiceCollection AddViewModelsModule(this IServiceCollection services)
    {
        services.AddTransient<ImportBookViewModel>();
        services.AddTransient<ImportWordViewModel>();
        services.AddTransient<WordPacksViewModel>();
        services.AddTransient<TypingLauncherViewModel>();
        services.AddTransient<ExercisesManagerViewModel>();
        services.AddTransient<WordsExplorerViewModel>();
        services.AddSingleton<ThemeViewModel>();
        services.AddSingleton<AccentColorViewModel>();
        return services;
    }
}
