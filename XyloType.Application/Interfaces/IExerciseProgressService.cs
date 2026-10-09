using XyloType.Application.Models;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Application.Interfaces;

/// <summary>
/// The results of the exercises of the current user: what was done, and how well.
/// </summary>
public interface IExerciseProgressService
{
    /// <summary>
    /// Keeps the result of an exercise typed to the end by the current user.
    /// </summary>
    Task<Result<ExerciseAttemptOutcome>> RecordAsync(Guid exerciseId, TypingSessionResult result);

    /// <summary>
    /// The scores of the current user, per exercise done (empty without user).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ScoreSummary>> GetProgressAsync();
}
