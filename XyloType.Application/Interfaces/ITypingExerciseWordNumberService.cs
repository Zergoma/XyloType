namespace XyloType.Application.Interfaces;

public interface ITypingExerciseWordNumberService
{
    /// <summary>
    /// Number of words per line generated for a dynamic exercise (always > 0).
    /// </summary>
    int ItemNumber { get; }

    /// <summary>
    /// Validates and saves the number of words per line.
    /// </summary>
    /// <returns>Fail if the value is not strictly positive</returns>
    Result<bool> SetItemNumber(int itemNumber);
}
