using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XyloType.Application.Models;

/// <summary>
/// A word pack ready to download: the words of a language with their number of occurrences,
/// taken from public domain texts, so that the app works at once without importing books.
/// </summary>
/// <param name="LanguageCode">ISO 639-1</param>
/// <param name="Title">Shown to the user and kept in the import history</param>
/// <param name="Version">Version of the pack (date of its build, e.g. 2026.10.08)</param>
/// <param name="FileName">The file to download, next to the catalog</param>
/// <param name="Sha256">Checksum of the file, checked after the download</param>
/// <param name="Size">Size of the file, in bytes</param>
/// <param name="WordCount">Distinct words in the pack</param>
/// <param name="Sources">The texts the words come from</param>
public record WordPackInfo(
    string LanguageCode,
    string Title,
    string Version,
    string FileName,
    string Sha256,
    long Size,
    int WordCount,
    IReadOnlyList<string> Sources);

/// <summary>
/// The packs available, published as <see cref="FileName"/> next to the pack files.
/// </summary>
public record WordPackCatalog(int Format, IReadOnlyList<WordPackInfo> Packs)
{
    public const string FileName = "word-packs.json";

    /// <summary>
    /// Format understood by this version of the app: a newer catalog is refused, not misread.
    /// </summary>
    public const int CurrentFormat = 1;

    private static readonly JsonSerializerOptions s_json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // accents written as is: the catalog stays readable
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string ToJson() => JsonSerializer.Serialize(this, s_json);

    public static WordPackCatalog? FromJson(string json) => JsonSerializer.Deserialize<WordPackCatalog>(json, s_json);
}

/// <summary>
/// File format of a pack: UTF-8 text compressed with gzip, one word per line, "word TAB occurrences",
/// most frequent first; lines starting with '#' are comments.
/// </summary>
public static class WordPackFormat
{
    public const char Separator = '\t';
    public const char Comment = '#';

    public static string FileNameFor(string languageCode) => $"words-{languageCode}.tsv.gz";
}
