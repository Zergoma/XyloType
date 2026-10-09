using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces;

/// <summary>
/// The exercises typed to the end, per user.
/// </summary>
public interface IExerciseAttemptRepository
{
    Task AddAsync(ExerciseAttempt attempt);

    /// <summary>
    /// The scores of every attempt of the user, per exercise.
    /// </summary>
    Task<Dictionary<Guid, List<double>>> GetScoresByExerciseAsync(int userId);

    /// <summary>
    /// Attempts per user (users without any are left out).
    /// </summary>
    Task<Dictionary<int, int>> CountByUserAsync();
}
