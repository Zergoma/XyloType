namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// What is good and what is too much at a level: the time to find a key, and the share of its occurrences missed.
/// Between the two, a result goes from good to bad.
/// </summary>
/// <param name="GoodResponseSeconds">At most: good (e.g. 0.6 s a key is about 20 words per minute)</param>
/// <param name="SlowResponseSeconds">At least: too slow</param>
/// <param name="GoodErrorPercent">At most: good</param>
/// <param name="BadErrorPercent">At least: too many errors</param>
public record TypingTargets(
    double GoodResponseSeconds,
    double SlowResponseSeconds,
    double GoodErrorPercent,
    double BadErrorPercent)
{
    public static TypingTargets For(TypingLevel level) => level switch
    {
        // 20, 40 and 60 words per minute: 0.6, 0.3 and 0.2 s a key
        TypingLevel.Beginner => new(0.6, 1.5, 10, 40),
        TypingLevel.Expert => new(0.2, 0.5, 2, 12),
        _ => new(0.3, 0.8, 5, 25),
    };

    /// <summary>
    /// From 0 (good) to 1 (too slow).
    /// </summary>
    public double ResponseBadness(double seconds) => Badness(seconds, GoodResponseSeconds, SlowResponseSeconds);

    /// <summary>
    /// From 0 (good) to 1 (too many errors).
    /// </summary>
    public double ErrorBadness(double percent) => Badness(percent, GoodErrorPercent, BadErrorPercent);

    private static double Badness(double value, double good, double bad)
        => Math.Clamp((value - good) / (bad - good), 0, 1);
}
