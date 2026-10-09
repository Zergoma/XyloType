using XyloType.Domain.Typing;

namespace XyloType.Application.Models;

/// <summary>
/// The names of the levels, as shown and as written in the exercise packs.
/// </summary>
public static class TypingLevelNames
{
    public static IReadOnlyList<TypingLevel> All { get; } = [TypingLevel.Beginner, TypingLevel.Intermediate, TypingLevel.Expert];

    public static string Of(TypingLevel level) => level switch
    {
        TypingLevel.Beginner => "Débutant",
        TypingLevel.Expert => "Confirmé",
        _ => "Intermédiaire",
    };

    /// <summary>
    /// The level of a name (French or English, any case); intermediate when unknown.
    /// </summary>
    public static TypingLevel Parse(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "débutant" or "debutant" or "beginner" => TypingLevel.Beginner,
        "confirmé" or "confirme" or "expert" => TypingLevel.Expert,
        _ => TypingLevel.Intermediate,
    };
}
