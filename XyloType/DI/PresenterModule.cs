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
        services.AddTransient<IThemeIconeCodeProvider, ThemeIconeCodeProvider>();


        // TODO
        // move or delete
        // Preferences are not as flexible as expected
        // need more standar way, completely out of maui
        services.AddTransient<IUserKeyboardLayoutPreferenceService, MauiUserKeyboardLayoutPreferenceService>();
        services.AddTransient<IUserTypingPreferenceService, MauiUserTypingPreferenceService>();
        return services;
    }
}
