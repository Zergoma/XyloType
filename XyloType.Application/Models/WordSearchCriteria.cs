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
}
