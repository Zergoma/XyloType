using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

using Xylocopadream.UI.Avalonia.Dialogs;

using XyloType.Application.DI;
using XyloType.Application.Interfaces;
using XyloType.Desktop.Navigation;
using XyloType.Desktop.Services;
using XyloType.Desktop.Views;
using XyloType.Infrastructure.DbContexts;
using XyloType.Infrastructure.DI;
using XyloType.Infrastructure.IO;
using XyloType.Infrastructure.Settings;
using XyloType.ViewModels.DI;

namespace XyloType.Desktop.Composition;

/// <summary>
/// The services of the desktop app: the shared layers (infrastructure, application, view models),
/// then what is its own (settings file, assets folder, Avalonia services, views).
/// </summary>
public static class DesktopServices
{
    public static ServiceProvider Build()
    {
        AppPaths.EnsureCreated();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            // one line per SQL command (37 000 to install the words): only the problems
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "XyloType.Desktop")
            .WriteTo.File(Path.Combine(AppPaths.Logs, "XyloType-.txt"), rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024, rollOnFileSizeLimit: true, retainedFileCountLimit: 10)
            .CreateLogger();

        ServiceCollection services = new();
        services.AddLogging(logging => logging.ClearProviders().AddSerilog(Log.Logger, dispose: true));

        // shared layers
        services
            .AddXyloTypeInfrastructure()
            .AddXyloTypeApplication()
            .AddViewModelsModule();

        services.AddDbContextFactory<DactyloDbContext>(options => options.UseSqlite($"Data Source={AppPaths.Database}"));

        // what the shared layers ask from the app
        services.AddSingleton<ISettingsStore>(provider =>
            new JsonSettingsStore(AppPaths.Settings, provider.GetRequiredService<ILogger<JsonSettingsStore>>()));
        services.AddSingleton<IAssetReader>(new FileAssetReader(AppPaths.Assets));
        services.AddSingleton<IThemeChangerService, AvaloniaThemeService>();
        services.AddSingleton<AvaloniaAccentColorService>();
        services.AddSingleton<IAccentColorService>(provider => provider.GetRequiredService<AvaloniaAccentColorService>());
        services.AddTransient<IUserDialogService, AvaloniaUserDialogService>();
        services.AddTransient<IChoosePath, AvaloniaChooseFilePresenter>();
        services.AddTransient<INavigationService, AvaloniaNavigationService>();
        services.AddTransient<ExerciseViewFactory>();

        // the dialogs open over the main window
        services.AddSingleton<IDialogService>(provider => new DialogService(() => provider.GetService<MainWindow>()));

        // the window, its navigation and the views of the sections
        services.AddSingleton<AppNavigator>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<UsersView>();
        services.AddTransient<TypingLauncherView>();
        services.AddTransient<ExercisesView>();
        services.AddTransient<ExercisesManagerView>();
        services.AddTransient<ExercisePacksView>();
        services.AddTransient<WordsExplorerView>();
        services.AddTransient<ImportView>();
        services.AddTransient<ImportWordView>();
        services.AddTransient<ImportBookView>();

        return services.BuildServiceProvider();
    }
}
