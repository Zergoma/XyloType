using XyloType.Application.DTOs;

namespace XyloType.Application.Models.Typing.Exercices;

/// <summary>
/// The exercises of a keyboard, in sections.
/// The exercises are kept in the order of their sections: the index of an exercise is its place in the lists.
/// </summary>
public class TypingExercices
{
    public required KeyBoardLayoutDto KeyboardLayout { get; set; }

    public List<ExerciseSection> Sections { get; set; } = [];

    public List<TypingExercise> Exercices { get; set; } = [];

    /// <summary>
    /// Every exercise in a known section (the ones without go to "Mes exercices", created if needed),
    /// and the exercises in the order of the sections, the order inside a section kept.
    /// </summary>
    public void NormalizeSections(Func<Guid> newId)
    {
        HashSet<Guid> known = [.. Sections.Select(s => s.Id)];
        List<TypingExercise> orphans = [.. Exercices.Where(e => !known.Contains(e.SectionId))];

        if (orphans.Count > 0)
        {
            ExerciseSection own = Sections.Find(s => s.PackId is null && s.Title == ExerciseSection.DefaultTitle)
                ?? AddSection(ExerciseSection.DefaultTitle, newId());

            foreach (TypingExercise exercise in orphans)
                exercise.SectionId = own.Id;
        }

        Dictionary<Guid, int> order = Sections
            .Select((section, index) => (section.Id, index))
            .ToDictionary(s => s.Id, s => s.index);

        // OrderBy is stable: the order inside a section is kept
        Exercices = [.. Exercices.OrderBy(e => order[e.SectionId])];
    }

    public ExerciseSection AddSection(string title, Guid id)
    {
        ExerciseSection section = new() { Id = id, Title = title };
        Sections.Add(section);
        return section;
    }

    public IEnumerable<TypingExercise> ExercisesOf(Guid sectionId)
        => Exercices.Where(e => e.SectionId == sectionId);
}
