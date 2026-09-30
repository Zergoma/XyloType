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
}
