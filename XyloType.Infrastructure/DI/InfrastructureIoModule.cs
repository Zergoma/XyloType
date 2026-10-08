using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Infrastructure.IO;
using XyloType.Infrastructure.Theme.Loaders;
using XyloType.Infrastructure.Theme.Providers;

namespace XyloType.Infrastructure.DI;

internal static class InfrastructureIoModule
{
    public static IServiceCollection AddIo(this IServiceCollection services)
    {
        services.AddTransient<IWordStreamReader, TextFileWordReader>();
        services.AddTransient<IWordPackReader, WordPackReader>();
        services.AddSingleton<IWordPackSource, GitHubWordPackSource>();
        services.AddTransient<IContentHasher, NormalizedTextHasher>();

        services.AddSingleton<AssetThemesLoader>();
        services.AddSingleton<UserThemesLoader>();

        services.AddSingleton<ITypingThemeProvider, TypingThemeProvider>();
        return services;
    }
}