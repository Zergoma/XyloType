using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Factories;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;

namespace XyloType.Application.DI;

internal static class ApplicationFactoriesModule
{
    public static IServiceCollection AddXyloTypeApplicationFactories(this IServiceCollection services)
    {
        services.AddTransient<IKeyBoardLayoutDtoFactory, KeyBoardLayoutDtoFactory>();
        services.AddTransient<ITypingExerciceSettingFactory, TypingExerciceSettingFactory>();
        return services;
    }
}