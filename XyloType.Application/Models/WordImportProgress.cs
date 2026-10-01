namespace XyloType.Application.Models;

/// <summary>
/// Progress of a word import.
/// </summary>
/// <param name="WordsRead">Words read so far</param>
/// <param name="Fraction">Share of the file already read, from 0 to 1</param>
public record WordImportProgress(int WordsRead, double Fraction);
