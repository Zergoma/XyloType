namespace XyloType.Application.Interfaces;

/// <summary>
/// User settings of the typing screen, persisted between sessions.
/// Volumes range from 0 (muted) to 1.
/// </summary>
public interface IUserTypingPreferenceService
{
    double GetOkVolume();
    void SetOkVolume(double volume);

    double GetErrorVolume();
    void SetErrorVolume(double volume);

    bool GetBackReturnEnable();
    void SetBackReturnEnable(bool enable);

    bool GetStopOnError();
    void SetStopOnError(bool enable);
}
