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
            { UsesLeftHand: true, UsesRightHand: false } => "gauche",
            { UsesLeftHand: false, UsesRightHand: true } => "droite",
            _ => "deux",
        };

        HandsTooltip = analysis switch
        {
            null => "",
            { UsesLeftHand: true, UsesRightHand: false } => "Main gauche seule",
            { UsesLeftHand: false, UsesRightHand: true } => "Main droite seule",
            _ => "Les deux mains",
        };

        if (analysis?.ExternalAccent == true)
            HandsText += " · morte";

        if (analysis?.ExternalAccent == true)
            HandsTooltip += ", avec une touche morte (accent tapé avant la lettre)";
    }

    public int Id { get; }

    public string Text { get; }

    public int Occurrences { get; }

    public int Length { get; }

    public string LanguageCode { get; }

    public string HandsText { get; }

    public string HandsTooltip { get; }

    public bool HasHands => HandsText.Length > 0;

    /// <summary>
    /// Language code shown in capitals ("FR").
    /// </summary>
    public string LanguageLabel => LanguageCode.ToUpperInvariant();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    public partial bool IsExcluded { get; set; }

    public bool IsActive => !IsExcluded;
}
