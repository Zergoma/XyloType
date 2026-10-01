using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// User settings of the typing screens, persisted between sessions.
/// Volumes range from 0 (muted) to 1.
/// </summary>
public interface IUserTypingPreferenceService
{
    public const int DefaultLineNumber = 5;
    public const int DefaultWordNumber = 20;

    double GetOkVolume();
    void SetOkVolume(double volume);

    double GetErrorVolume();
    void SetErrorVolume(double volume);

    bool GetBackReturnEnable();
    void SetBackReturnEnable(bool enable);

    bool GetStopOnError();
    void SetStopOnError(bool enable);

    int GetLineNumber();
    void SetLineNumber(int lineNumber);

    int GetWordNumber();
    void SetWordNumber(int wordNumber);

    bool GetShowSpeedResult();
    void SetShowSpeedResult(bool show);

    bool GetShowResponseTimeResult();
    void SetShowResponseTimeResult(bool show);

    bool GetShowErrorsResult();
    void SetShowErrorsResult(bool show);

    OkSoundMode GetOkSoundMode();
    void SetOkSoundMode(OkSoundMode mode);

    ScoreEndBehavior GetScoreEndBehavior();
    void SetScoreEndBehavior(ScoreEndBehavior behavior);

    /// <summary>
    /// True: the next piece is picked at random; false: the pieces follow the catalog order.
    /// </summary>
    bool GetScoreShuffle();
    void SetScoreShuffle(bool shuffle);

    InstrumentChoice GetInstrument();
    void SetInstrument(InstrumentChoice instrument);

    /// <summary>
    /// Instruments left out of the random pick.
    /// </summary>
    IReadOnlySet<InstrumentChoice> GetDisabledInstruments();
    void SetDisabledInstruments(IEnumerable<InstrumentChoice> instruments);

    /// <summary>
    /// Ids of the scores the user does not want to hear.
    /// </summary>
    IReadOnlySet<string> GetDisabledScores();
    void SetDisabledScores(IEnumerable<string> scoreIds);
}
