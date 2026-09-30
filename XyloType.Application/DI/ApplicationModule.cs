using Microsoft.Extensions.DependencyInjection;

namespace XyloType.Application.DI;

static public class ApplicationModule
{
    public static IServiceCollection AddXyloTypeApplication(this IServiceCollection services)
    {
        services.AddXyloTypeApplicationFactories();
        services.AddXyloTypeApplicationValidators();
        services.AddXyloTypeApplicationServices();
        services.AddXyloTypeApplicationManagers();
        services.AddXyloTypeApplicationOrchestrators();
        services.AddXyloTypeApplicationUseCases();
        
        return services;
    }
}
