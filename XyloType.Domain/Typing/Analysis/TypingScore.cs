namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// The score of a typing session: net words per minute, the speed lowered by the share of errors.
/// Typing fast with many errors scores less than typing a bit slower without.
/// </summary>
public static class TypingScore
{
    /// <param name="wordsPerMinute">Speed, five characters a word</param>
    /// <param name="accuracy">Share of the characters typed right the first time, from 0 to 1</param>
    public static double From(double wordsPerMinute, double accuracy)
        => Math.Round(Math.Max(0, wordsPerMinute) * Math.Clamp(accuracy, 0, 1), 1);
}
