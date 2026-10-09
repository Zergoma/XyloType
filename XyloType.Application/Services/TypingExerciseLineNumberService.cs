using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Application.Services;

public class TypingExerciseLineNumberService : ITypingExerciseLineNumberService
{
    private readonly IUserTypingPreferenceService _typingPreference;

    public TypingExerciseLineNumberService(IUserTypingPreferenceService typingPreference)
    {
        _typingPreference = typingPreference;
    }

    /// <summary>
    /// The saved number of lines, within the limits (a value saved before they existed may be bigger).
    /// </summary>
    public int LineNumber
    {
        get
        {
            int saved = _typingPreference.GetLineNumber();
            return saved > 0 ? ExerciseSizeLimits.ClampLines(saved) : IUserTypingPreferenceService.DefaultLineNumber;
        }
    }

    public Result<bool> SetLineNumber(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > ExerciseSizeLimits.MaxLines)
        {
            return Result<bool>
                .Fail($"Le nombre de lignes doit être entre 1 et {ExerciseSizeLimits.MaxLines}");
        }

        _typingPreference.SetLineNumber(lineNumber);
        return Result<bool>.Ok(true);
    }
}
