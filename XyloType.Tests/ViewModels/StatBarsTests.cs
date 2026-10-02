using FluentAssertions;

using XyloType.ViewModels.Statistic;

namespace XyloType.Tests.ViewModels;

public class StatBarsTests
{
    private static readonly KeyValue[] s_values =
    [
        new('a', 0.40), new('e', 0.45), new('s', 0.90), new(' ', 0.38), new('k', 1.20), new('j', 0.85),
    ];

    [Fact]
    public void WithoutGrouping_EachKeyHasItsRow_SlowestFirst()
    {
        IReadOnlyList<KeyGroup> groups = StatBars.Group(s_values, 0.1, group: false);

        groups.Select(g => StatBars.Label(g.Keys)).Should().Equal("k", "s", "j", "e", "a", "␣");
    }

    [Fact]
    public void WithGrouping_CloseValuesShareARow()
    {
        IReadOnlyList<KeyGroup> groups = StatBars.Group(s_values, 0.1, group: true);

        groups.Select(g => StatBars.Label(g.Keys)).Should().Equal("k", "s j", "e a ␣");
        groups[1].Value.Should().BeApproximately(0.875, 0.0001);
        groups[0].IsSingle.Should().BeTrue();
    }
}
