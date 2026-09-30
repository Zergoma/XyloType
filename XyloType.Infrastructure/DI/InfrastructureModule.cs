using Microsoft.Extensions.DependencyInjection;

namespace XyloType.Infrastructure.DI;

public static class InfrastructureModule
{
    public static IServiceCollection AddXyloTypeInfrastructure(this IServiceCollection services)
    {
        services.AddProviders();
        services.AddRepositories();
        services.AddIo();
        services.AddTheme();
        services.AddStrores();

        return services;
    }
}
