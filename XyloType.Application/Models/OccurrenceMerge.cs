namespace XyloType.Application.Models;

/// <summary>
/// How the occurrences read for a word already in the database are combined with its count.
/// </summary>
public enum OccurrenceMerge
{
    /// <summary>
    /// A text adds its occurrences: each book makes the words it contains more frequent.
    /// </summary>
    Add,

    /// <summary>
    /// A word pack already holds totals: the highest count is kept, so importing a pack again
    /// (or a newer version of it) does not count the same texts twice.
    /// </summary>
    KeepHighest,
}
