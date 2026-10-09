using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XyloType.Application.Models;

/// <summary>
/// An exercise pack ready to download: sections of exercises for a keyboard, from the first keys to whole texts.
/// </summary>
/// <param name="Id">Stable name of the pack, e.g. "azerty-fr-debutant"</param>
/// <param name="Title">Shown to the user</param>
/// <param name="Level">Débutant, Intermédiaire, Confirmé</param>
/// <param name="Layout">The keyboard (<c>KeyboardLayoutEnumDto</c> name, e.g. "AzertyFr")</param>
/// <param name="Version">Version of the pack (date of its last change, e.g. 2026.10.09)</param>
/// <param name="FileName">The file to download, next to the catalog</param>
/// <param name="Sha256">Checksum of the file, checked after the download</param>
public record ExercisePackInfo(
    string Id,
    string Title,
    string Level,
    string Description,
    string Layout,
    string Version,
    string FileName,
    string Sha256,
    long Size,
    int ExerciseCount);

/// <summary>
/// The exercise packs available, published as <see cref="FileName"/> next to the pack files.
/// </summary>
public record ExercisePackCatalog(int Format, IReadOnlyList<ExercisePackInfo> Packs)
{
    public const string FileName = "exercise-packs.json";

    /// <summary>
    /// Format understood by this version of the app: a newer catalog is refused, not misread.
    /// </summary>
    public const int CurrentFormat = 1;

    public string ToJson() => JsonSerializer.Serialize(this, ExercisePackJson.Options);

    public static ExercisePackCatalog? FromJson(string json) => JsonSerializer.Deserialize<ExercisePackCatalog>(json, ExercisePackJson.Options);
}

/// <summary>
/// The content of a pack file (JSON). Sections and exercises are named by keys,
/// from which their ids are derived (<see cref="ExercisePackIds"/>): an update of the pack updates them in place,
/// and the results of the users stay with them.
/// </summary>
public record ExercisePack(
    int Format,
    string Id,
    string Title,
    string Level,
    string Description,
    string Layout,
    string Version,
    IReadOnlyList<ExercisePackSection> Sections)
{
    public int ExerciseCount => Sections.Sum(s => s.Exercises.Count);

    public static ExercisePack? FromJson(string json) => JsonSerializer.Deserialize<ExercisePack>(json, ExercisePackJson.Options);

    public string ToJson() => JsonSerializer.Serialize(this, ExercisePackJson.Options);
}

public record ExercisePackSection(string Key, string Title, IReadOnlyList<ExercisePackExercise> Exercises);

/// <summary>
/// An exercise: a fixed text (<see cref="Text"/>, one line per item), or words generated at each game (<see cref="Generator"/>).
/// </summary>
/// <param name="Letters">Allowed letters; for a fixed text, the letters of the text when left out</param>
public record ExercisePackExercise(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<string>? Text = null,
    string? Letters = null,
    ExercisePackGenerator? Generator = null);

/// <param name="Source">"pseudoWords" (invented from the letters) or "words" (real words of the imported dictionary)</param>
public record ExercisePackGenerator(string Source, int LengthMin, int LengthMax, IReadOnlyList<string>? Languages = null)
{
    public const string PseudoWords = "pseudoWords";
    public const string Words = "words";
}

/// <summary>
/// Ids of the sections and exercises of a pack, derived from their keys: the same at each import.
/// </summary>
public static class ExercisePackIds
{
    public static Guid Section(string packId, string sectionKey) => From($"xylotype:{packId}/section/{sectionKey}");

    public static Guid Exercise(string packId, string exerciseKey) => From($"xylotype:{packId}/exercise/{exerciseKey}");

    private static Guid From(string name)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes(name)).AsSpan(0, 16));
}

internal static class ExercisePackJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // accents written as is: the packs stay readable
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
