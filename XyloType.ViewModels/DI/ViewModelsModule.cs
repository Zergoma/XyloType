using Microsoft.Extensions.DependencyInjection;

using XyloType.ViewModels.Theme;
using XyloType.ViewModels.ExercisesManager;
using XyloType.ViewModels.Import;
using XyloType.ViewModels.WordsExplorer;
using XyloType.ViewModels.TypingLauncher;
using XyloType.ViewModels.Users;


namespace XyloType.ViewModels.DI;

public static class ViewModelsModule
{
    public static IServiceCollection AddViewModelsModule(this IServiceCollection services)
    {
        services.AddTransient<ImportBookViewModel>();
        services.AddTransient<ImportWordViewModel>();
        services.AddTransient<WordPacksViewModel>();
        services.AddTransient<StarterPacksViewModel>();
        services.AddTransient<TypingLauncherViewModel>();
        services.AddTransient<ExercisesManagerViewModel>();
        services.AddTransient<ExercisePacksViewModel>();
        services.AddTransient<WordsExplorerViewModel>();
        services.AddTransient<ExerciseViewModelFactory>();
        services.AddSingleton<ThemeViewModel>();
        services.AddSingleton<AccentColorViewModel>();

        // one list of users, shared by their section and the quick switch of the rail
        services.AddSingleton<UsersViewModel>();
        return services;
    }
}
