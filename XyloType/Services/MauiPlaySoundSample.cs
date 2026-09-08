using Plugin.Maui.Audio;

using XyloType.Application.Interfaces;

namespace XyloType.Services;

public class MauiPlaySoundSample : IPlaySoundSample
{
    public async Task PLaySoundAsync(string sound, double volume)
    {
        var audioPlayer =
            AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync(sound));
        audioPlayer?.Volume = volume;
        audioPlayer?.Play();
    }
}
