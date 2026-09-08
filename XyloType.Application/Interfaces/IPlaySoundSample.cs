namespace XyloType.Application.Interfaces;

public interface IPlaySoundSample
{
    public Task PLaySoundAsync(string sound, double volume);
}
