using XyloType.Domain.Typing.Analysis;

namespace XyloType.Domain.Entities;

/// <summary>
/// An exercise typed to the end by a user.
/// </summary>
public class ExerciseAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public UserProfile? User { get; set; }

    /// <summary>
    /// Id of the exercise (exercises live in the exercise files, not in the database).
    /// </summary>
    public Guid ExerciseId { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public double DurationSeconds { get; set; }

    public int Characters { get; set; }

    public int CharactersWithError { get; set; }

    public int WrongKeyPresses { get; set; }

    public double WordsPerMinute { get; set; }

    /// <summary>
    /// From 0 to 1.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// See <see cref="TypingScore"/>.
    /// </summary>
    public double Score { get; set; }

    public static ExerciseAttempt From(int userId, Guid exerciseId, TypingSessionResult result, DateTime completedAtUtc)
        => new()
        {
            UserId = userId,
            ExerciseId = exerciseId,
            CompletedAtUtc = completedAtUtc,
            DurationSeconds = result.Duration.TotalSeconds,
            Characters = result.Characters,
            CharactersWithError = result.CharactersWithError,
            WrongKeyPresses = result.WrongKeyPresses,
            WordsPerMinute = Math.Round(result.WordsPerMinute, 1),
            Accuracy = result.Accuracy,
            Score = result.Score,
        };
}
