namespace XyloType.ViewModels.Statistic;

/// <summary>
/// One row of a horizontal bar chart: a key (or several keys with close values), its bar and its value.
/// </summary>
/// <param name="Label">The key(s), special keys shown as symbols</param>
/// <param name="Value">Value of the row (average of the group)</param>
/// <param name="ValueText">Value shown at the end of the bar</param>
/// <param name="Ratio">Bar length, from 0 to 1 (1 for the biggest value of the chart)</param>
/// <param name="ColorHex">Color of the bar</param>
public record StatBarItem(string Label, double Value, string ValueText, double Ratio, string ColorHex);

/// <summary>
/// A value measured for one key.
/// </summary>
public record KeyValue(char Key, double Value);

/// <summary>
/// Keys whose values are close, shown as a single row.
/// </summary>
public record KeyGroup(IReadOnlyList<KeyValue> Keys)
{
    public double Value => Keys.Average(k => k.Value);

    public bool IsSingle => Keys.Count == 1;
}

public static class StatBars
{
    /// <summary>
    /// Sorts the values from the biggest, and groups the keys that are close to each other:
    /// a group gathers the keys that are within <paramref name="tolerance"/> of its first (biggest) value.
    /// Without grouping, each key is alone in its group.
    /// </summary>
    public static IReadOnlyList<KeyGroup> Group(IEnumerable<KeyValue> values, double tolerance, bool group)
    {
        List<KeyValue> sorted = [.. values.OrderByDescending(v => v.Value).ThenBy(v => v.Key)];

        if (!group)
            return [.. sorted.Select(v => new KeyGroup([v]))];

        List<KeyGroup> groups = [];
        List<KeyValue> current = [];

        foreach (KeyValue value in sorted)
        {
            if (current.Count > 0 && current[0].Value - value.Value > tolerance)
            {
                groups.Add(new KeyGroup(current));
                current = [];
            }

            current.Add(value);
        }

        if (current.Count > 0)
            groups.Add(new KeyGroup(current));

        return groups;
    }

    /// <summary>
    /// The keys of a row, readable: space, enter and tab shown as symbols.
    /// </summary>
    public static string Label(IEnumerable<KeyValue> keys)
        => string.Join(" ", keys.Select(k => k.Key switch
        {
            ' ' => "␣",
            '\n' => "↵",
            '\t' => "⇥",
            _ => k.Key.ToString(),
        }));
}
