using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Managers;
using XyloType.Application.Services;

namespace XyloType.Application.DI;

internal static class ApplicationManagersModule
{
    public static IServiceCollection AddXyloTypeApplicationManagers(this IServiceCollection services)
    {
        services.AddTransient<ITypingExercicesManager, TypingExercicesManager>();
        services.AddTransient<IKeyboardKeyLocatorManager, KeyboardKeyLocatorManager>(); //IKeyboardKeysLocator

        return services;
    }
}