using XyloType.Application.Interfaces;
using XyloType.Services;

namespace XyloType.DI;

public static class UiServicesModule
{
    public static IServiceCollection AddMauiService(this IServiceCollection services)
    {
        // depends on ViewFactoriesModule
        services.AddTransient<INavigationService, MauiNavigationService>();
        services.AddTransient<IUserDialogService, MauiUserDialogService>();
        services.AddSingleton<MauiAccentColorService>();
        services.AddSingleton<IAccentColorService>(provider => provider.GetRequiredService<MauiAccentColorService>());

        return services;
    }
}
