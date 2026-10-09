using CommunityToolkit.Maui;


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


using Serilog;
using Serilog.Events;

using XyloType.Application.DI;
using XyloType.DI;
using XyloType.Infrastructure.DbContexts;
using XyloType.Infrastructure.DI;
using XyloType.ViewModels.DI;


namespace XyloType;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var logDirectory =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger =
            new LoggerConfiguration()
                .MinimumLevel.Information()
                // one line per SQL command (37 000 to install the words): only the problems
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "XyloType")
                .WriteTo.Console()

                .WriteTo.File(
                    Path.Combine(
                        logDirectory,
                        "XyloType-.txt"),
                    rollingInterval: RollingInterval.Day)
#if DEBUG
                    .WriteTo.Seq("http://localhost:5341")
#endif
                .CreateLogger();


        var builder = MauiApp.CreateBuilder();

        builder.Logging
                .ClearProviders()
                .AddSerilog(Log.Logger);

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        Log.Information(
        "Application started {ApplicationName} {Version}",
        "XyloType",
        AppInfo.Current.VersionString);


        builder.Services
            .AddMauiInfrastructure()        // declare a IAssetReader
            .AddXyloTypeInfrastructure()    // need a IAssetReader

            // presenters are used inside App Orchestrators
            .AddMauiPresenters()

            .AddXyloTypeApplication()
            .AddViewModelsModule()

            .AddMauiViewFactories()
            .AddMauiService()
            .AddMauiViews();

        // DB context factory
        string databasePath =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "dactylo.db3");

        builder.Services.AddDbContextFactory<DactyloDbContext>(
            options =>
                options.UseSqlite($"Data Source={databasePath}"));

        var app = builder.Build();

        // INFRASTRUCTURE
        // DB: init or upgrade according to the migration state (errors are logged)
        InfrastructureDbInitModule.InitUpgradeInfrastructure(app.Services);

        return app;
    }
}
