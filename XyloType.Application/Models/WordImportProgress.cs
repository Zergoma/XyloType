namespace XyloType.Application.Models;

/// <summary>
/// Progress of a word import.
/// </summary>
/// <param name="Phase">Current step</param>
/// <param name="WordsRead">Words read so far</param>
/// <param name="Fraction">Progress of the current step, from 0 to 1</param>
public record WordImportProgress(WordImportPhase Phase, int WordsRead, double Fraction);

public enum WordImportPhase
{
    /// <summary>
    /// Reading and analyzing the file (nothing written yet).
    /// </summary>
    Reading,

    /// <summary>
    /// Writing the words in the database (one transaction).
    /// </summary>
    Saving
}
