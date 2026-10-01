using System.Globalization;
using System.Text;

namespace XyloType.Domain.Text;

/// <summary>
/// Compares text titles (usually file names) to spot the same book imported twice.
/// </summary>
public static class TitleSimilarity
{
    /// <summary>
    /// From this score, two titles are considered close.
    /// </summary>
    public const double CloseThreshold = 0.8;

    // words that only mark a copy or a version of the same file
    private static readonly HashSet<string> s_noiseWords =
        ["copie", "copy", "final", "new", "nouveau", "edition", "ed", "version"];

    /// <summary>
    /// Lower case, no accent, no punctuation, no copy marker ("(1)", "copie", "v2"...).
    /// </summary>
    public static string Normalize(string title)
    {
        string decomposed = title.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        StringBuilder builder = new(decomposed.Length);
        foreach (char c in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue; // accent

            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        IEnumerable<string> words = builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !s_noiseWords.Contains(w)
                        && !w.All(char.IsDigit)                                 // "(1)", "2"
                        && !(w.Length > 1 && w[0] == 'v' && w[1..].All(char.IsDigit))); // "v2"

        return string.Join(' ', words);
    }

    /// <summary>
    /// Similarity of two titles, from 0 (different) to 1 (same once normalized).
    /// </summary>
    public static double Score(string title, string other)
    {
        string a = Normalize(title);
        string b = Normalize(other);

        if (a.Length == 0 || b.Length == 0)
            return 0;

        if (a == b)
            return 1;

        // "les miserables" / "victor hugo les miserables tome 1"
        if (Math.Min(a.Length, b.Length) >= 4 && (a.Contains(b) || b.Contains(a)))
            return 0.9;

        return 1.0 - (double)Levenshtein(a, b) / Math.Max(a.Length, b.Length);
    }

    public static bool AreClose(string title, string other)
        => Score(title, other) >= CloseThreshold;

    private static int Levenshtein(string a, string b)
    {
        int[] previous = new int[b.Length + 1];
        int[] current = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
