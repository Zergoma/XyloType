namespace XyloType.Application.Models;

/// <summary>
/// Outcome of a word import.
/// </summary>
/// <param name="WordsRead">Words read in the file (with repetitions)</param>
/// <param name="NewWords">Distinct words added to the database</param>
/// <param name="UpdatedWords">Distinct words already known (occurrence count increased)</param>
/// <param name="IgnoredWords">Distinct words that cannot be typed with the keyboard</param>
/// <param name="IgnoredSample">Some of the ignored words, to show the user why</param>
public record WordImportSummary(
    int WordsRead,
    int NewWords,
    int UpdatedWords,
    int IgnoredWords,
    IReadOnlyList<string> IgnoredSample);
