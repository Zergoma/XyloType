using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Infrastructure.Repositories;

namespace XyloType.Infrastructure.DI;

internal static class InfrastructureRepositoriesModule
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient<IDactyloRepository, DactyloRepository>();
        services.AddTransient<IImportedSourceRepository, ImportedSourceRepository>();
        return services;
    }
}