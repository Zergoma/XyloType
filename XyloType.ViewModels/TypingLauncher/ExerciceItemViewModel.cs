using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;

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

    [ObservableProperty] public partial bool IsSelected { get; set; }

    public string Name => _exercice.Name;

    public Guid Guid => _exercice.Id;

    public string Desciption => _exercice.Description;


    public string Letters => _exercice.AllowedCharacters;

    public bool IsStatic => _isStatic;

    public bool IsDynamic => _isDynamic;

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

}
