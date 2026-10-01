using XyloType.Domain.Enums;

namespace XyloType.Application.Models;

/// <summary>
/// Criteria of the imported words to pick.
/// </summary>
/// <param name="Languages">Language codes, all languages if empty</param>
/// <param name="AllowedLetters">Every character of a word must be one of these</param>
/// <param name="MinLength">Minimum word length</param>
/// <param name="MaxLength">Maximum word length</param>
/// <param name="Layout">Keyboard the words have been analyzed for</param>
public record ImportedWordsOptions(
    IReadOnlyList<string> Languages,
    string AllowedLetters,
    int MinLength,
    int MaxLength,
    KeyboardLayout Layout);
