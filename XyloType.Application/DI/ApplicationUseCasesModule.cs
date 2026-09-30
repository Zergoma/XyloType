using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.UseCases;

namespace XyloType.Application.DI;

static internal class ApplicationUseCasesModule
{
    public static IServiceCollection AddXyloTypeApplicationUseCases(this IServiceCollection services)
    {
        services.AddTransient<ISaveTypingExerciceUseCase, SaveTypingExerciceUseCase>();

        return services;
    }

}
