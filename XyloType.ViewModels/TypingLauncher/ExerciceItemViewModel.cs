using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.ViewModels.TypingLauncher;

public partial class ExerciceItemViewModel : ObservableObject
{
    private readonly TypingExercise _exercice;
    private readonly int _idx;
    private readonly bool _isStatic;
    private readonly bool _isDynamic;
    public ExerciceItemViewModel(TypingExercise exercice, int idx)
    {
        _exercice = exercice;
        _idx = idx;
        IsSelected = false;
        _isStatic = _exercice.TextDataType is TypingTextDataStatic;
        _isDynamic = _exercice.TextDataType is TypingTextDataDynamic;
    }

    public int Idx => _idx;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSelected))]
    public partial bool IsSelected { get; set; }

    public bool IsNotSelected => !IsSelected;

    public string Name => _exercice.Name;

    public Guid Guid => _exercice.Id;

    public Guid SectionId => _exercice.SectionId;

    public string Desciption => _exercice.Description;


    public string Letters => _exercice.AllowedCharacters;

    public bool IsStatic => _isStatic;

    public bool IsDynamic => _isDynamic;

    /// <summary>
    /// Lines of a fixed text shown in the preview, at most.
    /// </summary>
    public const int PreviewMaxLines = 20;

    private string[] TextLines
        => _exercice.TextDataType is TypingTextDataStatic data
            ? data.GeneratedText.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
            : [];

    /// <summary>
    /// A fixed text with something to show.
    /// </summary>
    public bool HasTextPreview => IsStatic && TextLines.Any(l => !string.IsNullOrWhiteSpace(l));

    /// <summary>
    /// The first lines of a fixed text.
    /// </summary>
    public string TextPreview => string.Join('\n', TextLines.Take(PreviewMaxLines));

    public bool IsTextPreviewTruncated => TextLines.Length > PreviewMaxLines;

    public string TextPreviewTruncatedText => $"… texte tronqué : {PreviewMaxLines} lignes sur {TextLines.Length}";

    /// <summary>
    /// Words drawn again at each game, from the imported dictionary (real words).
    /// </summary>
    public bool IsRealWords => _exercice.TextDataType is TypingTextDataDynamic { GeneratedTypeSource: GeneratedTypeSource.Words };

    /// <summary>
    /// Words invented from the allowed letters, drawn again at each game.
    /// </summary>
    public bool IsPseudoWords => IsDynamic && !IsRealWords;

    /// <summary>
    /// Badge of the generated exercises: new words at each game.
    /// </summary>
    public string BadgeText => IsRealWords ? "Vrais mots" : IsPseudoWords ? "Mots inventés" : string.Empty;

    #region Results of the current user

    /// <summary>
    /// Scores of the attempts of the current user, null if the exercise was never typed to the end.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDone))]
    [NotifyPropertyChangedFor(nameof(IsNotDone))]
    [NotifyPropertyChangedFor(nameof(BestText))]
    [NotifyPropertyChangedFor(nameof(AttemptsText))]
    [NotifyPropertyChangedFor(nameof(WorstText))]
    [NotifyPropertyChangedFor(nameof(MedianText))]
    public partial ScoreSummary? Progress { get; set; }

    public bool IsDone => Progress is not null;

    public bool IsNotDone => !IsDone;

    public string BestText => Progress is ScoreSummary p ? $"{p.Best:N1}" : string.Empty;

    public string WorstText => Progress is ScoreSummary p ? $"{p.Worst:N1}" : string.Empty;

    public string MedianText => Progress is ScoreSummary p ? $"{p.Median:N1}" : string.Empty;

    public string AttemptsText => Progress?.Attempts switch
    {
        null => "Pas encore fait",
        1 => "Fait 1 fois",
        int n => $"Fait {n} fois",
    };

    #endregion

}
