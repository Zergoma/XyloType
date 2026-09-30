namespace XyloType.Application.Interfaces;

public interface ITypingExerciseLineNumberService
{
    /// <summary>
    /// Number of lines generated for a dynamic exercise (always > 0).
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Validates and saves the number of lines.
    /// </summary>
    /// <returns>Fail if the value is not strictly positive</returns>
    Result<bool> SetLineNumber(int lineNumber);
}
