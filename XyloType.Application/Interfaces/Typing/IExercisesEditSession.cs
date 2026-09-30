using XyloType.Application.DTOs;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Interfaces.Typing;

/// <summary>
/// Edits the exercises of one keyboard in memory.
/// Nothing is written until <see cref="SaveAsync"/>; <see cref="DiscardAsync"/> reloads the saved file.
/// </summary>
public interface IExercisesEditSession
{
    KeyBoardLayoutDto? Keyboard { get; }

    IReadOnlyList<TypingExercise> Exercises { get; }

    /// <summary>
    /// True when the in-memory exercises differ from the saved file.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Loads the exercises of <paramref name="keyboard"/> (an empty list if none were saved yet).
    /// </summary>
    Task<Result<bool>> OpenAsync(KeyBoardLayoutDto keyboard);

    /// <summary>
    /// Adds a new static exercise at the end of the list.
    /// </summary>
    Result<TypingExercise> CreateNew(string name);

    /// <summary>
    /// Marks the session as modified after an exercise has been edited in place.
    /// Validation happens on save.
    /// </summary>
    void MarkChanged();

    Result<bool> Remove(Guid id);

    Result<bool> Move(int fromIndex, int toIndex);

    /// <summary>
    /// Validates every exercise, then writes the whole file.
    /// </summary>
    Task<Result<bool>> SaveAsync();

    /// <summary>
    /// Drops the in-memory changes and reloads the saved file.
    /// </summary>
    Task<Result<bool>> DiscardAsync();
}
