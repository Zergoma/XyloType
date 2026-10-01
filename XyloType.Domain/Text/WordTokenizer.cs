using System.Text.RegularExpressions;

using XyloType.Domain.Constaintes;

namespace XyloType.Domain.Text;

/// <summary>
/// Splits a text into lower case words, keeping the apostrophes and hyphens that belong to a word:
/// <list type="bullet">
/// <item>compound words stay whole: "peut-être", "arc-en-ciel"</item>
/// <item>elided articles and pronouns are dropped (French, Italian): "l'école" → "école", "jusqu'à" → "à"</item>
/// <item>other apostrophes stay in the word: "aujourd'hui", "presqu'île", "don't"</item>
/// <item>the French euphonic "-t-" splits the words: "a-t-il" → "a", "il"</item>
/// </list>
/// </summary>
public static partial class WordTokenizer
{
    // letters, optionally joined by an apostrophe or a hyphen (never at the start or the end)
    [GeneratedRegex(@"\p{L}+(?:['-]\p{L}+)*")]
    private static partial Regex WordRegex();

    private static readonly Dictionary<string, HashSet<string>> s_elisions = new()
    {
        [LanguageCodes.French] =
        [
            "l", "d", "j", "m", "n", "s", "t", "c",
            "qu", "jusqu", "lorsqu", "puisqu", "quoiqu"
        ],
        [LanguageCodes.Italian] =
        [
            "l", "c", "un", "dell", "nell", "all", "sull", "dall", "coll",
            "quest", "quell", "bell", "sant"
        ],
    };

    public static IEnumerable<string> Tokenize(string text, string languageCode)
    {
        string normalized = Normalize(text);
        s_elisions.TryGetValue(languageCode, out HashSet<string>? elisions);
        bool isFrench = languageCode == LanguageCodes.French;

        foreach (Match match in WordRegex().Matches(normalized))
        {
            string token = match.Value.ToLowerInvariant();

            IEnumerable<string> parts = isFrench ? SplitEuphonicT(token) : [token];

            foreach (string part in parts)
            {
                string word = elisions is null ? part : DropElision(part, elisions);
                if (word.Length > 0)
                    yield return word;
            }
        }
    }

    /// <summary>
    /// Typographic apostrophes and hyphens become the keyboard ones.
    /// </summary>
    private static string Normalize(string text)
        => text
            .Replace('’', '\'')
            .Replace('‘', '\'')
            .Replace('ʼ', '\'')
            .Replace('‐', '-')  // U+2010 hyphen
            .Replace('‑', '-'); // U+2011 non-breaking hyphen

    /// <summary>
    /// "a-t-il" → "a", "il" (the "t" only joins the verb and the pronoun).
    /// </summary>
    private static IEnumerable<string> SplitEuphonicT(string token)
        => token.Contains("-t-", StringComparison.Ordinal)
            ? token.Split("-t-", StringSplitOptions.RemoveEmptyEntries)
            : [token];

    /// <summary>
    /// Drops the leading elided words: "qu'il" → "il", "l'arc-en-ciel" → "arc-en-ciel".
    /// </summary>
    private static string DropElision(string token, HashSet<string> elisions)
    {
        int apostrophe = token.IndexOf('\'');
        while (apostrophe > 0 && elisions.Contains(token[..apostrophe]))
        {
            token = token[(apostrophe + 1)..];
            apostrophe = token.IndexOf('\'');
        }
        return token;
    }
}
