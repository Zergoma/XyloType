namespace XyloType.Domain.Typing.Analysis;

/// <summary>
/// The scores of the attempts of an exercise: the worst, the median and the best.
/// </summary>
public record ScoreSummary(int Attempts, double Worst, double Median, double Best)
{
    /// <returns>null when there is no score</returns>
    public static ScoreSummary? From(IEnumerable<double> scores)
    {
        double[] sorted = [.. scores.Order()];
        if (sorted.Length == 0)
            return null;

        int middle = sorted.Length / 2;
        double median = sorted.Length % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;

        return new ScoreSummary(sorted.Length, sorted[0], Math.Round(median, 1), sorted[^1]);
    }
}
