using XyloType.Application.DTOs;

namespace XyloType.Application.Models;

/// <summary>
/// The packs that make the app ready at once for a keyboard: the exercise packs not imported yet,
/// and a word pack of its language when the dictionary is empty.
/// </summary>
/// <param name="CatalogsAvailable">False without connection: nothing could be checked</param>
public record StarterPacksOffer(
    KeyBoardLayoutDto Keyboard,
    WordPackInfo? WordPack,
    IReadOnlyList<ExercisePackInfo> ExercisePacks,
    bool CatalogsAvailable)
{
    public bool IsEmpty => WordPack is null && ExercisePacks.Count == 0;
}
