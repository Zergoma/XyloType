using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Services;

public class MauiUserTypingPreferenceService : IUserTypingPreferenceService
{
    private const string OkVolumeKey = "sound_ok_volume";
    private const string ErrorVolumeKey = "sound_error_volume";
    private const string BackReturnEnableKey = "typing_back_return_enable";
    private const string StopOnErrorKey = "typing_stop_on_error";
    private const string LineNumberKey = "exercise_line_number";
    private const string WordNumberKey = "exercise_word_number";
    private const string ShowSpeedResultKey = "result_show_speed";
    private const string ShowResponseTimeResultKey = "result_show_response_time";
    private const string ShowErrorsResultKey = "result_show_errors";
    private const string OkSoundModeKey = "sound_ok_mode";
    private const string DisabledScoresKey = "sound_disabled_scores";
    private const string ScoreEndBehaviorKey = "sound_score_end";
    private const string ScoreShuffleKey = "sound_score_shuffle";
    private const string InstrumentKey = "sound_instrument";
    private const string DisabledInstrumentsKey = "sound_disabled_instruments";
    private const string ShowTypingProgressKey = "display_typing_progress";
    private const string ShowLiveSpeedKey = "display_live_speed";
    private const string ShowScoreProgressKey = "display_score_progress";
    private const string ShowScoreChangeMarkerKey = "display_score_change_marker";

    private const double DefaultOkVolume = .3;
    private const double DefaultErrorVolume = .4;
    private const bool DefaultBackReturnEnable = true;
    private const bool DefaultStopOnError = true;
    private const bool DefaultShowSpeedResult = true;
    private const bool DefaultShowResponseTimeResult = true;
    private const bool DefaultShowErrorsResult = true;

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

    public bool GetShowSpeedResult()
        => Preferences.Default.Get(ShowSpeedResultKey, DefaultShowSpeedResult);

    public void SetShowSpeedResult(bool show)
        => Preferences.Default.Set(ShowSpeedResultKey, show);

    public bool GetShowResponseTimeResult()
        => Preferences.Default.Get(ShowResponseTimeResultKey, DefaultShowResponseTimeResult);

    public void SetShowResponseTimeResult(bool show)
        => Preferences.Default.Set(ShowResponseTimeResultKey, show);

    public bool GetShowErrorsResult()
        => Preferences.Default.Get(ShowErrorsResultKey, DefaultShowErrorsResult);

    public void SetShowErrorsResult(bool show)
        => Preferences.Default.Set(ShowErrorsResultKey, show);

    public OkSoundMode GetOkSoundMode()
    {
        string saved = Preferences.Default.Get(OkSoundModeKey, nameof(OkSoundMode.Standard));

        // before songs and instrumental pieces were told apart, every piece was a "Score"
        if (saved == "Score")
            return OkSoundMode.Instrumental;

        return Enum.TryParse(saved, out OkSoundMode mode) ? mode : OkSoundMode.Standard;
    }

    public void SetOkSoundMode(OkSoundMode mode)
        => Preferences.Default.Set(OkSoundModeKey, mode.ToString());

    public ScoreEndBehavior GetScoreEndBehavior()
        => Enum.TryParse(Preferences.Default.Get(ScoreEndBehaviorKey, nameof(ScoreEndBehavior.Loop)), out ScoreEndBehavior behavior)
            ? behavior
            : ScoreEndBehavior.Loop;

    public void SetScoreEndBehavior(ScoreEndBehavior behavior)
        => Preferences.Default.Set(ScoreEndBehaviorKey, behavior.ToString());

    public bool GetScoreShuffle()
        => Preferences.Default.Get(ScoreShuffleKey, true);

    public void SetScoreShuffle(bool shuffle)
        => Preferences.Default.Set(ScoreShuffleKey, shuffle);

    public bool GetShowTypingProgress()
        => Preferences.Default.Get(ShowTypingProgressKey, true);

    public void SetShowTypingProgress(bool show)
        => Preferences.Default.Set(ShowTypingProgressKey, show);

    public bool GetShowLiveSpeed()
        => Preferences.Default.Get(ShowLiveSpeedKey, true);

    public void SetShowLiveSpeed(bool show)
        => Preferences.Default.Set(ShowLiveSpeedKey, show);

    public bool GetShowScoreProgress()
        => Preferences.Default.Get(ShowScoreProgressKey, true);

    public void SetShowScoreProgress(bool show)
        => Preferences.Default.Set(ShowScoreProgressKey, show);

    public bool GetShowScoreChangeMarker()
        => Preferences.Default.Get(ShowScoreChangeMarkerKey, true);

    public void SetShowScoreChangeMarker(bool show)
        => Preferences.Default.Set(ShowScoreChangeMarkerKey, show);

    public InstrumentChoice GetInstrument()
        => Enum.TryParse(Preferences.Default.Get(InstrumentKey, nameof(InstrumentChoice.Xylophone)), out InstrumentChoice instrument)
            ? instrument
            : InstrumentChoice.Xylophone;

    public void SetInstrument(InstrumentChoice instrument)
        => Preferences.Default.Set(InstrumentKey, instrument.ToString());

    public IReadOnlySet<InstrumentChoice> GetDisabledInstruments()
        => Preferences.Default.Get(DisabledInstrumentsKey, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => Enum.TryParse(name, out InstrumentChoice instrument) ? instrument : (InstrumentChoice?)null)
            .OfType<InstrumentChoice>()
            .ToHashSet();

    public void SetDisabledInstruments(IEnumerable<InstrumentChoice> instruments)
        => Preferences.Default.Set(DisabledInstrumentsKey, string.Join(',', instruments));

    public IReadOnlySet<string> GetDisabledScores()
        => Preferences.Default.Get(DisabledScoresKey, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

    public void SetDisabledScores(IEnumerable<string> scoreIds)
        => Preferences.Default.Set(DisabledScoresKey, string.Join(',', scoreIds));
}
