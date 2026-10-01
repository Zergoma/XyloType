using XyloType.Domain.Enums;

namespace XyloType.Domain.Entities;

public class Word
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// ISO 639-1
    /// fr, en, de, es...
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    public int Length { get; set; }

    /// <summary>
    /// Fréquence d'apparition du mot dans le corpus.
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// Excluded by the user: never used in exercises, kept excluded when imported again.
    /// </summary>
    public bool IsExcluded { get; set; }

    public ICollection<WordAnalysis> Analyses { get; set; }
        = [];

    public void AddAnalysis(WordAnalysis analysis)
    {
        analysis.Word = this;       // enforce link to current object
        Analyses.Add(analysis);     // add it to the collection
    }

    public bool AnalyseExists(KeyboardLayout layout)
        => Analyses.Any(a => a.Layout == layout);
}
