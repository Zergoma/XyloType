namespace XyloType.Application.Models.Typing.Exercices;

/// <summary>
/// A titled group of exercises (e.g. "Débutant · La rangée de repos"), shown as a heading in the lists.
/// </summary>
public class ExerciseSection
{
    /// <summary>
    /// Title of the section of the exercises written before the sections existed.
    /// </summary>
    public const string DefaultTitle = "Mes exercices";

    public required Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The exercise pack the section comes from, null for a section of the user.
    /// </summary>
    public string? PackId { get; set; }

    /// <summary>
    /// Version of the pack imported, to offer its updates.
    /// </summary>
    public string? PackVersion { get; set; }
}
