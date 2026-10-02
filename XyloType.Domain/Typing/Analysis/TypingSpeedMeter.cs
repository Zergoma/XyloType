namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// Typing speed over the last seconds, in words per minute (a word is 5 characters, the usual measure).
/// Times are given on the session clock, which stops during the pauses.
/// </summary>
public class TypingSpeedMeter
{
    public const int CharactersPerWord = 5;

    /// <summary>
    /// Below this, too few keys were typed for a meaningful speed.
    /// </summary>
    private static readonly TimeSpan s_minimumWindow = TimeSpan.FromSeconds(2);

    private readonly Queue<TimeSpan> _keys = new();

    public TypingSpeedMeter(TimeSpan window)
    {
        Window = window;
    }

    public TimeSpan Window { get; }

    /// <summary>
    /// A correct key typed at this time of the session.
    /// </summary>
    public void Record(TimeSpan at)
    {
        _keys.Enqueue(at);
        Forget(at);
    }

    public void Reset()
        => _keys.Clear();

    /// <summary>
    /// Words per minute over the window ending now, or null at the very start of the session.
    /// At the start the window is the time typed so far, so the speed is right from the first seconds.
    /// </summary>
    public double? WordsPerMinute(TimeSpan now)
    {
        Forget(now);

        TimeSpan window = now < Window ? now : Window;
        if (window < s_minimumWindow)
            return null;

        return _keys.Count / (double)CharactersPerWord / window.TotalMinutes;
    }

    private void Forget(TimeSpan now)
    {
        while (_keys.Count > 0 && now - _keys.Peek() > Window)
            _keys.Dequeue();
    }
}
