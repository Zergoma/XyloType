using CommunityToolkit.Mvvm.ComponentModel;

namespace XyloType.MVVM.Controls;

/// <summary>
/// Column widths of the words table, shared by its header and every row.
/// The short columns get a share of the width, kept between what their content needs and a maximum;
/// the word takes the rest, so it is never the one cut on a small window.
/// </summary>
public partial class WordsTableColumns : ObservableObject
{
    public const double ColumnSpacing = 10;

    // share of the width, minimum and maximum of each short column
    private static readonly (double Share, double Min, double Max) s_occurrences = (0.14, 58, 170);
    private static readonly (double Share, double Min, double Max) s_length = (0.12, 44, 150);
    private static readonly (double Share, double Min, double Max) s_hands = (0.15, 66, 190);
    private static readonly (double Share, double Min, double Max) s_language = (0.11, 44, 140);
    private const double ExcludeWidth = 84;

    // room always left to the word: the other columns shrink first
    private const double WordMinWidth = 110;

    [ObservableProperty] public partial GridLength Exclude { get; private set; } = new(ExcludeWidth);
    [ObservableProperty] public partial GridLength Occurrences { get; private set; } = new(s_occurrences.Min);
    [ObservableProperty] public partial GridLength Length { get; private set; } = new(s_length.Min);
    [ObservableProperty] public partial GridLength Hands { get; private set; } = new(s_hands.Min);
    [ObservableProperty] public partial GridLength Language { get; private set; } = new(s_language.Min);

    /// <summary>
    /// The word: all the room left.
    /// </summary>
    public GridLength Word { get; } = GridLength.Star;

    /// <summary>
    /// Widths for a table of the given inner width.
    /// </summary>
    public void Fit(double width)
    {
        if (width <= 0)
            return;

        double occurrences = Size(width, s_occurrences);
        double length = Size(width, s_length);
        double hands = Size(width, s_hands);
        double language = Size(width, s_language);

        // not enough room left for the word: the short columns give some back, down to their minimum
        double shorts = occurrences + length + hands + language;
        double left = width - ExcludeWidth - shorts - 5 * ColumnSpacing;
        if (left < WordMinWidth && shorts > 0)
        {
            double ratio = Math.Max(0, (shorts - (WordMinWidth - left)) / shorts);
            occurrences = Math.Max(s_occurrences.Min, occurrences * ratio);
            length = Math.Max(s_length.Min, length * ratio);
            hands = Math.Max(s_hands.Min, hands * ratio);
            language = Math.Max(s_language.Min, language * ratio);
        }

        Occurrences = new GridLength(occurrences);
        Length = new GridLength(length);
        Hands = new GridLength(hands);
        Language = new GridLength(language);
    }

    private static double Size(double width, (double Share, double Min, double Max) column)
        => Math.Clamp(width * column.Share, column.Min, column.Max);
}
