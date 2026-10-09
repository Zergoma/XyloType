using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Infrastructure.Audio;
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
        services.AddSingleton<IExercisePackSource, GitHubExercisePackSource>();
        services.AddTransient<IContentHasher, NormalizedTextHasher>();

        // one audio output for the app: the sounds start at once and can overlap (WASAPI: Windows only)
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IPlaySoundSample, NAudioSoundPlayer>();
        else
            services.AddSingleton<IPlaySoundSample, SilentSoundPlayer>();

        services.AddSingleton<AssetThemesLoader>();
        services.AddSingleton<UserThemesLoader>();

        services.AddSingleton<ITypingThemeProvider, TypingThemeProvider>();
        return services;
    }
}