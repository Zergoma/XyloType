namespace XyloType.Application.Models;

/// <summary>
/// The score of an attempt, compared with the previous ones of the user.
/// </summary>
/// <param name="PreviousBest">Null at the first attempt</param>
public record ExerciseAttemptOutcome(double Score, double? PreviousBest)
{
    /// <summary>
    /// Better than every previous attempt (not at the first one: there is nothing to beat).
    /// </summary>
    public bool IsRecord => PreviousBest is double best && Score > best;
}
