using XyloType.Domain.Entities;
using XyloType.Domain.Enums;

namespace XyloType.Application.Models;

public struct WordSearchCriteria
{
    public string[]? LanguagesCodes { get; set; }

    public KeyboardLayout? Layout { get; set; }

    public KeyboardRow? RowMask { get; set; }

    public bool? ExternalAccent { get; set; }

    public Finger? FingerMask { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    /// <summary>
    /// Loads the keyboard analyses of each word.
    /// </summary>
    public bool IncludeAnalyses { get; set; }

    /// <summary>
    /// Part of the word (case insensitive).
    /// </summary>
    public string? TextContains { get; set; }

    /// <summary>
    /// Every character of the word must be one of these.
    /// </summary>
    public string? OnlyLetters { get; set; }

    public int? MinOccurrences { get; set; }

    public int? MaxOccurrences { get; set; }

    /// <summary>
    /// Hands needed to type the word (requires <see cref="Layout"/> to be meaningful).
    /// </summary>
    public HandFilter Hands { get; set; }

    /// <summary>
    /// Excluded words are skipped by default.
    /// </summary>
    public WordExclusionFilter Exclusion { get; set; }
}

public enum WordExclusionFilter
{
    ActiveOnly = 0,
    All,
    ExcludedOnly
}

public enum HandFilter
{
    Any = 0,
    LeftOnly,
    RightOnly,
    BothHands
}

public enum WordSortField
{
    Occurrences = 0,
    Text,
    Length,
    Hands,
    Language,
    Excluded
}

/// <summary>
/// Sort of a word search: a column and a direction.
/// </summary>
/// <summary>
/// A word and its number of occurrences, without its keyboard analyses.
/// </summary>
public record WordFrequency(string Text, int OccurrenceCount);

public record WordSort(WordSortField Field, bool Descending)
{
    /// <summary>
    /// Most frequent words first.
    /// </summary>
    public static WordSort Default { get; } = new(WordSortField.Occurrences, Descending: true);
}

/// <summary>
/// One page of a word search.
/// </summary>
/// <param name="Words">Words of the page</param>
/// <param name="TotalCount">Number of words matching the criteria, all pages included</param>
public record WordSearchPage(List<Word> Words, int TotalCount);
