using XyloType.Application.Interfaces;

namespace XyloType.Application.Services;

public class TypingExerciseLineNumberService : ITypingExerciseLineNumberService
{
    private readonly IUserTypingPreferenceService _typingPreference;

    public TypingExerciseLineNumberService(IUserTypingPreferenceService typingPreference)
    {
        _typingPreference = typingPreference;
    }

    public int LineNumber
    {
        get
        {
            int saved = _typingPreference.GetLineNumber();
            return saved > 0 ? saved : IUserTypingPreferenceService.DefaultLineNumber;
        }
    }

    public Result<bool> SetLineNumber(int lineNumber)
    {
        if (lineNumber <= 0)
        {
            return Result<bool>
                .Fail("Le nombre de lignes doit être supérieur à 0");
        }

        _typingPreference.SetLineNumber(lineNumber);
        return Result<bool>.Ok(true);
    }
}
