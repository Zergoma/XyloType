using XyloType.Application.DTOs;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Interfaces.Typing;

/// <summary>
/// Edits the exercises of one keyboard in memory, in sections.
/// Nothing is written until <see cref="SaveAsync"/>; <see cref="DiscardAsync"/> reloads the saved file.
/// </summary>
public interface IExercisesEditSession
{
    KeyBoardLayoutDto? Keyboard { get; }

    IReadOnlyList<ExerciseSection> Sections { get; }

    /// <summary>
    /// In the order of their sections.
    /// </summary>
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
    /// Adds a new static exercise at the end of a section (null: the last section, created if there is none).
    /// </summary>
    Result<TypingExercise> CreateNew(string name, Guid? sectionId);

    /// <summary>
    /// Marks the session as modified after an exercise has been edited in place.
    /// Validation happens on save.
    /// </summary>
    void MarkChanged();

    Result<bool> Remove(Guid id);

    /// <summary>
    /// Moves an exercise inside its section (indexes in the section).
    /// </summary>
    Result<bool> Move(Guid sectionId, int fromIndex, int toIndex);

    /// <summary>
    /// Moves an exercise to the end of another section.
    /// </summary>
    Result<bool> MoveToSection(Guid exerciseId, Guid sectionId);

    Result<ExerciseSection> AddSection(string title);

    Result<bool> RenameSection(Guid sectionId, string title);

    /// <summary>
    /// Removes a section and its exercises.
    /// </summary>
    Result<bool> RemoveSection(Guid sectionId);

    /// <summary>
    /// Moves a section up (-1) or down (+1), with its exercises.
    /// </summary>
    Result<bool> MoveSection(Guid sectionId, int offset);

    /// <summary>
    /// Validates every exercise, then writes the whole file.
    /// </summary>
    Task<Result<bool>> SaveAsync();

    /// <summary>
    /// Drops the in-memory changes and reloads the saved file.
    /// </summary>
    Task<Result<bool>> DiscardAsync();
}
