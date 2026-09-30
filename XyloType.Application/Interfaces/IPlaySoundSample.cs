namespace XyloType.Application.Interfaces;

public interface IPlaySoundSample
{
    /// <summary>
    /// Loads the sounds once so that later calls to <see cref="PlaySound"/> start instantly.
    /// Already loaded sounds are skipped.
    /// </summary>
    public Task PreloadAsync(params string[] sounds);

    /// <summary>
    /// Plays a preloaded sound. A sound that was not preloaded is loaded in the background
    /// and skipped this time.
    /// </summary>
    public void PlaySound(string sound, double volume);
}
