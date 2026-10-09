using XyloType.Application.DTOs;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Domain.Typing;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Keeps track of the exercise being played, so that the result screen can retry it
/// or chain to the next one, and the launcher can reselect it when coming back.
/// </summary>
public interface ITypingExerciseRunService
{
    /// <summary>
    /// Id of the last exercise started, null if none.
    /// </summary>
    Guid? CurrentExerciseId { get; }

    /// <summary>
    /// Name of the last exercise started, empty if none.
    /// </summary>
    string CurrentExerciseName { get; }

    bool HasNext { get; }

    /// <summary>
    /// Level of the last exercise started (the one of its section), intermediate if none.
    /// </summary>
    TypingLevel CurrentLevel { get; }

    /// <summary>
    /// Starts the exercise at <paramref name="idx"/> and creates its text.
    /// </summary>
    Result<IStringsProvider> Start(TypingExercices exercises, int idx, KeyBoardLayoutDto keyboard);

    /// <summary>
    /// Creates the text of the current exercise again (generated exercises get new words).
    /// </summary>
    Result<IStringsProvider> Retry();

    /// <summary>
    /// Moves to the next exercise of the list and creates its text.
    /// </summary>
    Result<IStringsProvider> Next();
}
