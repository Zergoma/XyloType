using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Domain.Entities;
using XyloType.Domain.Enums;

namespace XyloType.ViewModels.WordsExplorer;

/// <summary>
/// One word of the explorer results.
/// </summary>
public partial class WordItemViewModel : ObservableObject
{
    public WordItemViewModel(Word word, KeyboardLayout? layout)
    {
        Id = word.Id;
        Text = word.Text;
        Occurrences = word.OccurrenceCount;
        Length = word.Length;
        LanguageCode = word.LanguageCode;
        IsExcluded = word.IsExcluded;

        WordAnalysis? analysis =
            word.Analyses.FirstOrDefault(a => layout is null || a.Layout == layout);

        HandsText = analysis switch
        {
            null => "",
            { UsesLeftHand: true, UsesRightHand: false } => "main gauche",
            { UsesLeftHand: false, UsesRightHand: true } => "main droite",
            _ => "deux mains",
        };

        if (analysis?.ExternalAccent == true)
            HandsText += " · touche morte";
    }

    public int Id { get; }

    public string Text { get; }

    public int Occurrences { get; }

    public int Length { get; }

    public string LanguageCode { get; }

    public string HandsText { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    public partial bool IsExcluded { get; set; }

    public bool IsActive => !IsExcluded;
}
