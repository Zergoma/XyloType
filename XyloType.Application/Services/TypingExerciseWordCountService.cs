using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Application.Services;

public class TypingExerciseWordNumberService : ITypingExerciseWordNumberService
{
    private readonly IUserTypingPreferenceService _typingPreference;

    public TypingExerciseWordNumberService(IUserTypingPreferenceService typingPreference)
    {
        _typingPreference = typingPreference;
    }

    /// <summary>
    /// The saved number of words per line, within the limits (a value saved before they existed may be bigger).
    /// </summary>
    public int ItemNumber
    {
        get
        {
            int saved = _typingPreference.GetWordNumber();
            return saved > 0 ? ExerciseSizeLimits.ClampWordsPerLine(saved) : IUserTypingPreferenceService.DefaultWordNumber;
        }
    }

    public Result<bool> SetItemNumber(int itemNumber)
    {
        if (itemNumber < 1 || itemNumber > ExerciseSizeLimits.MaxWordsPerLine)
        {
            return Result<bool>
                .Fail($"Le nombre de mots par ligne doit être entre 1 et {ExerciseSizeLimits.MaxWordsPerLine}");
        }

        _typingPreference.SetWordNumber(itemNumber);
        return Result<bool>.Ok(true);
    }
}
