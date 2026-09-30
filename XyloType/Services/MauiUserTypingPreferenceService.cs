using XyloType.Application.Interfaces;

namespace XyloType.Services;

public class MauiUserTypingPreferenceService : IUserTypingPreferenceService
{
    private const string OkVolumeKey = "sound_ok_volume";
    private const string ErrorVolumeKey = "sound_error_volume";
    private const string BackReturnEnableKey = "typing_back_return_enable";
    private const string StopOnErrorKey = "typing_stop_on_error";
    private const string LineNumberKey = "exercise_line_number";
    private const string WordNumberKey = "exercise_word_number";

    private const double DefaultOkVolume = .3;
    private const double DefaultErrorVolume = .4;
    private const bool DefaultBackReturnEnable = true;
    private const bool DefaultStopOnError = true;

    public double GetOkVolume()
        => Preferences.Default.Get(OkVolumeKey, DefaultOkVolume);

    public void SetOkVolume(double volume)
        => Preferences.Default.Set(OkVolumeKey, Math.Clamp(volume, 0, 1));

    public double GetErrorVolume()
        => Preferences.Default.Get(ErrorVolumeKey, DefaultErrorVolume);

    public void SetErrorVolume(double volume)
        => Preferences.Default.Set(ErrorVolumeKey, Math.Clamp(volume, 0, 1));

    public bool GetBackReturnEnable()
        => Preferences.Default.Get(BackReturnEnableKey, DefaultBackReturnEnable);

    public void SetBackReturnEnable(bool enable)
        => Preferences.Default.Set(BackReturnEnableKey, enable);

    public bool GetStopOnError()
        => Preferences.Default.Get(StopOnErrorKey, DefaultStopOnError);

    public void SetStopOnError(bool enable)
        => Preferences.Default.Set(StopOnErrorKey, enable);

    public int GetLineNumber()
        => Preferences.Default.Get(LineNumberKey, IUserTypingPreferenceService.DefaultLineNumber);

    public void SetLineNumber(int lineNumber)
        => Preferences.Default.Set(LineNumberKey, lineNumber);

    public int GetWordNumber()
        => Preferences.Default.Get(WordNumberKey, IUserTypingPreferenceService.DefaultWordNumber);

    public void SetWordNumber(int wordNumber)
        => Preferences.Default.Set(WordNumberKey, wordNumber);
}
