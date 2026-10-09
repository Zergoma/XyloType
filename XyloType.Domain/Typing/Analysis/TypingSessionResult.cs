namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// Outcome of a typing session.
/// </summary>
/// <param name="CharStats">Statistics per typed character</param>
/// <param name="Duration">Time from the first key press to the last character, pauses excluded</param>
public record TypingSessionResult(
    Dictionary<char, CharStats> CharStats,
    TimeSpan Duration)
{
    /// <summary>
    /// A word is five characters, spaces included (the usual typing speed unit).
    /// </summary>
    public const double CharactersPerWord = 5.0;

    /// <summary>
    /// Characters typed.
    /// </summary>
    public int Characters => CharStats.Values.Sum(s => s.NbOccurence);

    /// <summary>
    /// Characters that needed at least one retry.
    /// </summary>
    public int CharactersWithError => CharStats.Values.Sum(s => s.NbCharError);

    /// <summary>
    /// Wrong key presses (a character can be missed several times).
    /// </summary>
    public int WrongKeyPresses => CharStats.Values.Sum(s => s.RealErrors.Count);

    public double CharactersPerMinute => Duration.TotalMinutes > 0 ? Characters / Duration.TotalMinutes : 0;

    public double WordsPerMinute => CharactersPerMinute / CharactersPerWord;

    /// <summary>
    /// Share of the characters typed right the first time, from 0 to 1.
    /// </summary>
    public double Accuracy => Characters > 0 ? (double)(Characters - CharactersWithError) / Characters : 1;

    /// <summary>
    /// The score of the session: the speed, lowered by the errors (net words per minute).
    /// </summary>
    public double Score => TypingScore.From(WordsPerMinute, Accuracy);
}
