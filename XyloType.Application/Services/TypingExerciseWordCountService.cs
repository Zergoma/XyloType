using XyloType.Application.Interfaces;

namespace XyloType.Application.Services;

public class TypingExerciseWordNumberService : ITypingExerciseWordNumberService
{
    private readonly IUserTypingPreferenceService _typingPreference;

    public TypingExerciseWordNumberService(IUserTypingPreferenceService typingPreference)
    {
        _typingPreference = typingPreference;
    }

    public int ItemNumber
    {
        get
        {
            int saved = _typingPreference.GetWordNumber();
            return saved > 0 ? saved : IUserTypingPreferenceService.DefaultWordNumber;
        }
    }

    public Result<bool> SetItemNumber(int itemNumber)
    {
        if (itemNumber <= 0)
        {
            return Result<bool>
                .Fail("Le nombre de mots doit être supérieur à 0");
        }

        _typingPreference.SetWordNumber(itemNumber);
        return Result<bool>.Ok(true);
    }
}
