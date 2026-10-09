using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;

namespace XyloType.Infrastructure.Audio;

/// <summary>
/// No sound: the systems without an audio output for the app yet (WASAPI is Windows only).
/// The typing works the same, silently.
/// </summary>
public sealed class SilentSoundPlayer(ILogger<SilentSoundPlayer> logger) : IPlaySoundSample
{
    public Task PreloadAsync(params string[] sounds)
    {
        logger.LogInformation("No audio output on {System}: the sounds are not played", Environment.OSVersion.Platform);
        return Task.CompletedTask;
    }

    public void PlaySound(string sound, double volume)
    {
    }
}
