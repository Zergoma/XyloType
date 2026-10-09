using XyloType.Application.Models.Typing;

namespace XyloType.Application.Models.Typing.Exercices;


public class TypingExercise
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;

    public string AllowedCharacters { get; set; } = string.Empty;
    public TypingTextData TextDataType { get; set; }

    /// <summary>
    /// The section the exercise belongs to (see <see cref="TypingExercices.Sections"/>).
    /// </summary>
    public Guid SectionId { get; set; }

}

public union TypingTextData(TypingTextDataStatic, TypingTextDataDynamic);

public class TypingTextDataStatic
{
    public string GeneratedText { get; set; } = string.Empty;
}

public class TypingTextDataDynamic
{
    public GeneratedTypeSource GeneratedTypeSource { get; set; }
    public int LengthMin { get; set; }
    public int LengthMax { get; set; }

    public List<string> LanguagesSelected { get; set; } = [];
}