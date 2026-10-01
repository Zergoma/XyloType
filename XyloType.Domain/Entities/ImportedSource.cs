namespace XyloType.Domain.Entities;

/// <summary>
/// A text (book, word list...) whose words have been imported.
/// </summary>
public class ImportedSource
{
    public int Id { get; set; }

    /// <summary>
    /// Shown to the user, from the file name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 of the normalized text: same text, same hash, whatever the encoding or the line endings.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// ISO 639-1
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    public DateTime ImportedAtUtc { get; set; }

    /// <summary>
    /// Words read in the text, with repetitions.
    /// </summary>
    public int WordsRead { get; set; }

    public int NewWords { get; set; }

    public int UpdatedWords { get; set; }

    /// <summary>
    /// Distinct words that cannot be typed with the keyboard.
    /// </summary>
    public int IgnoredWords { get; set; }
}
