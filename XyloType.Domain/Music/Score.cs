namespace XyloType.Domain.Music;

/// <summary>
/// The melody of a piece of music, as MIDI note numbers (the rhythm is given by the typing).
/// </summary>
/// <param name="Id">Stable identifier, used in the user preferences</param>
/// <param name="Title">Name of the piece</param>
/// <param name="Composer">Its composer, or its origin for a traditional song</param>
/// <param name="Notes">The melody, one note per correct key</param>
/// <param name="Category">Kind of music (classical, traditional, Christmas...)</param>
public record Score(string Id, string Title, string Composer, IReadOnlyList<int> Notes, string Category = ScoreCategories.Classical);

/// <summary>
/// Kinds of music of the catalog, in display order.
/// </summary>
public static class ScoreCategories
{
    public const string Classical = "Classique";
    public const string Traditional = "Traditionnel";
    public const string Ragtime = "Ragtime";
    public const string Christmas = "Noël";
    public const string AmericanFolk = "Folk américain";

    public static IReadOnlyList<string> DisplayOrder { get; } =
        [Classical, Traditional, Ragtime, Christmas, AmericanFolk];
}
