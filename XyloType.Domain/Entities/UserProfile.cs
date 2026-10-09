namespace XyloType.Domain.Entities;

/// <summary>
/// A person using the app on this computer: the results of the exercises are kept per user.
/// </summary>
public class UserProfile
{
    public const int NameMaxLength = 40;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public List<ExerciseAttempt> Attempts { get; set; } = [];
}
