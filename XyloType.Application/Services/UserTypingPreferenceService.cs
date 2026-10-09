using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Application.Services;

/// <summary>
/// The settings of the typing screens, in the settings store of the app.
/// </summary>
public class UserTypingPreferenceService : IUserTypingPreferenceService
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
    private const string GroupResponseTimesKey = "result_group_response_times";
    private const string GroupErrorsKey = "result_group_errors";

    private const double DefaultOkVolume = .3;
    private const double DefaultErrorVolume = .4;
    private const bool DefaultBackReturnEnable = true;
    private const bool DefaultStopOnError = true;
    private const bool DefaultShowSpeedResult = true;
    private const bool DefaultShowResponseTimeResult = true;
    private const bool DefaultShowErrorsResult = true;

    private readonly ISettingsStore _store;

    public UserTypingPreferenceService(ISettingsStore store)
    {
        _store = store;
    }

    public double GetOkVolume()
        => _store.Get(OkVolumeKey, DefaultOkVolume);

    public void SetOkVolume(double volume)
        => _store.Set(OkVolumeKey, Math.Clamp(volume, 0, 1));

    public double GetErrorVolume()
        => _store.Get(ErrorVolumeKey, DefaultErrorVolume);

    public void SetErrorVolume(double volume)
        => _store.Set(ErrorVolumeKey, Math.Clamp(volume, 0, 1));

    public bool GetBackReturnEnable()
        => _store.Get(BackReturnEnableKey, DefaultBackReturnEnable);

    public void SetBackReturnEnable(bool enable)
        => _store.Set(BackReturnEnableKey, enable);

    public bool GetStopOnError()
        => _store.Get(StopOnErrorKey, DefaultStopOnError);

    public void SetStopOnError(bool enable)
        => _store.Set(StopOnErrorKey, enable);

    public int GetLineNumber()
        => _store.Get(LineNumberKey, IUserTypingPreferenceService.DefaultLineNumber);

    public void SetLineNumber(int lineNumber)
        => _store.Set(LineNumberKey, lineNumber);

    public int GetWordNumber()
        => _store.Get(WordNumberKey, IUserTypingPreferenceService.DefaultWordNumber);

    public void SetWordNumber(int wordNumber)
        => _store.Set(WordNumberKey, wordNumber);

    public bool GetShowSpeedResult()
        => _store.Get(ShowSpeedResultKey, DefaultShowSpeedResult);

    public void SetShowSpeedResult(bool show)
        => _store.Set(ShowSpeedResultKey, show);

    public bool GetShowResponseTimeResult()
        => _store.Get(ShowResponseTimeResultKey, DefaultShowResponseTimeResult);

    public void SetShowResponseTimeResult(bool show)
        => _store.Set(ShowResponseTimeResultKey, show);

    public bool GetShowErrorsResult()
        => _store.Get(ShowErrorsResultKey, DefaultShowErrorsResult);

    public void SetShowErrorsResult(bool show)
        => _store.Set(ShowErrorsResultKey, show);

    public bool GetGroupResponseTimes()
        => _store.Get(GroupResponseTimesKey, false);

    public void SetGroupResponseTimes(bool group)
        => _store.Set(GroupResponseTimesKey, group);

    public bool GetGroupErrors()
        => _store.Get(GroupErrorsKey, false);

    public void SetGroupErrors(bool group)
        => _store.Set(GroupErrorsKey, group);

    public OkSoundMode GetOkSoundMode()
    {
        string saved = _store.Get(OkSoundModeKey, nameof(OkSoundMode.Standard));

        // before songs and instrumental pieces were told apart, every piece was a "Score"
        if (saved == "Score")
            return OkSoundMode.Instrumental;

        return Enum.TryParse(saved, out OkSoundMode mode) ? mode : OkSoundMode.Standard;
    }

    public void SetOkSoundMode(OkSoundMode mode)
        => _store.Set(OkSoundModeKey, mode.ToString());

    public ScoreEndBehavior GetScoreEndBehavior()
        => Enum.TryParse(_store.Get(ScoreEndBehaviorKey, nameof(ScoreEndBehavior.Loop)), out ScoreEndBehavior behavior)
            ? behavior
            : ScoreEndBehavior.Loop;

    public void SetScoreEndBehavior(ScoreEndBehavior behavior)
        => _store.Set(ScoreEndBehaviorKey, behavior.ToString());

    public bool GetScoreShuffle()
        => _store.Get(ScoreShuffleKey, true);

    public void SetScoreShuffle(bool shuffle)
        => _store.Set(ScoreShuffleKey, shuffle);

    public bool GetShowTypingProgress()
        => _store.Get(ShowTypingProgressKey, true);

    public void SetShowTypingProgress(bool show)
        => _store.Set(ShowTypingProgressKey, show);

    public bool GetShowLiveSpeed()
        => _store.Get(ShowLiveSpeedKey, true);

    public void SetShowLiveSpeed(bool show)
        => _store.Set(ShowLiveSpeedKey, show);

    public bool GetShowScoreProgress()
        => _store.Get(ShowScoreProgressKey, true);

    public void SetShowScoreProgress(bool show)
        => _store.Set(ShowScoreProgressKey, show);

    public bool GetShowScoreChangeMarker()
        => _store.Get(ShowScoreChangeMarkerKey, true);

    public void SetShowScoreChangeMarker(bool show)
        => _store.Set(ShowScoreChangeMarkerKey, show);

    public InstrumentChoice GetInstrument()
        => Enum.TryParse(_store.Get(InstrumentKey, nameof(InstrumentChoice.Xylophone)), out InstrumentChoice instrument)
            ? instrument
            : InstrumentChoice.Xylophone;

    public void SetInstrument(InstrumentChoice instrument)
        => _store.Set(InstrumentKey, instrument.ToString());

    public IReadOnlySet<InstrumentChoice> GetDisabledInstruments()
        => _store.Get(DisabledInstrumentsKey, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => Enum.TryParse(name, out InstrumentChoice instrument) ? instrument : (InstrumentChoice?)null)
            .OfType<InstrumentChoice>()
            .ToHashSet();

    public void SetDisabledInstruments(IEnumerable<InstrumentChoice> instruments)
        => _store.Set(DisabledInstrumentsKey, string.Join(',', instruments));

    public IReadOnlySet<string> GetDisabledScores()
        => _store.Get(DisabledScoresKey, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

    public void SetDisabledScores(IEnumerable<string> scoreIds)
        => _store.Set(DisabledScoresKey, string.Join(',', scoreIds));
}
