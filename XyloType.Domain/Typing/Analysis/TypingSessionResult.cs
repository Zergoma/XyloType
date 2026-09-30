namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// Outcome of a typing session.
/// </summary>
/// <param name="CharStats">Statistics per typed character</param>
/// <param name="Duration">Time from the first key press to the last character, pauses excluded</param>
public record TypingSessionResult(
    Dictionary<char, CharStats> CharStats,
    TimeSpan Duration);
