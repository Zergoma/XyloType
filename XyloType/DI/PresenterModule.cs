using XyloType.Application.Interfaces;
using XyloType.Presenter;
using XyloType.Services;

namespace XyloType.DI;

public static class PresenterModule
{
    public static IServiceCollection AddMauiPresenters(this IServiceCollection services)
    {
        services.AddTransient<IChoosePath, MauiChooseFilePresenter>();
        services.AddTransient<IThemeChangerService, MauiThemeChangerService>();

        // the settings of the user, in the MAUI preferences (the services reading them are in the Application layer)
        services.AddSingleton<ISettingsStore, MauiSettingsStore>();

        return services;
    }
}
