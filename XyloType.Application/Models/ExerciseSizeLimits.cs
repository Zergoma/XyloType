namespace XyloType.Application.Models;

/// <summary>
/// Size of a generated exercise: a limit keeps the exercise reasonable to type and quick to prepare
/// (the words are picked when it starts). Used by the home page, the saved preferences and the exercise editor.
/// </summary>
public static class ExerciseSizeLimits
{
    public const int MaxLines = 20;

    public const int MaxWordsPerLine = 20;

    /// <summary>
    /// Characters of a fixed text (marks ↵ and ⟶ included): a generated text of the largest size fits.
    /// </summary>
    public const int MaxTextLength = 4000;

    public static string TextHint { get; } = $"Au plus {MaxTextLength:N0} caractères";

    /// <summary>
    /// The lines of a generated text that fit within <see cref="MaxTextLength"/>: whole lines, no word cut.
    /// </summary>
    public static IEnumerable<string> FitLines(IEnumerable<string> lines, string separator)
    {
        int length = 0;
        foreach (string line in lines)
        {
            length += (length == 0 ? 0 : separator.Length) + line.Length;
            if (length > MaxTextLength)
                yield break;

            yield return line;
        }
    }

    /// <summary>
    /// The allowed range, shown in the tooltips of the fields.
    /// </summary>
    public static string LinesHint { get; } = $"De 1 à {MaxLines} lignes";

    public static string WordsPerLineHint { get; } = $"De 1 à {MaxWordsPerLine} mots par ligne";

    public static int ClampLines(int lines) => Math.Clamp(lines, 1, MaxLines);

    public static int ClampWordsPerLine(int words) => Math.Clamp(words, 1, MaxWordsPerLine);
}
