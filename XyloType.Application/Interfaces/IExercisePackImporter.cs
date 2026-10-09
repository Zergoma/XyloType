using XyloType.Application.DTOs;
using XyloType.Application.Models;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Adds the sections and exercises of a pack to the exercises of a keyboard, or updates them.
/// </summary>
public interface IExercisePackImporter
{
    /// <summary>
    /// Converts a pack into exercises, checked like the ones of the editor.
    /// </summary>
    Result<IReadOnlyList<(ExerciseSection Section, IReadOnlyList<TypingExercise> Exercises)>> Convert(ExercisePack pack);

    /// <summary>
    /// Merges the pack into the saved exercises of the keyboard and saves them.
    /// The exercises of the user are left as they are.
    /// </summary>
    Task<Result<ExercisePackImportSummary>> ImportAsync(ExercisePack pack, KeyBoardLayoutDto keyboard);

    /// <summary>
    /// The version of each pack imported for the keyboard (pack id → version).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetImportedVersionsAsync(KeyBoardLayoutDto keyboard);
}
