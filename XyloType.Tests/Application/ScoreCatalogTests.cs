using FluentAssertions;

using XyloType.Application.Services;
using XyloType.Domain.Music;

namespace XyloType.Tests.Application;

public class ScoreCatalogTests
{
    [Fact]
    public void Songs_AreToldFromInstrumentalPieces()
    {
        IReadOnlyList<Score> scores = new ScoreCatalog().GetAll();

        scores.Single(s => s.Id == "trad-au-clair-de-la-lune").IsSong.Should().BeTrue();
        scores.Single(s => s.Id == "gruber-silent-night").IsSong.Should().BeTrue();
        scores.Single(s => s.Id == "beethoven-fur-elise").IsSong.Should().BeFalse();
        scores.Where(s => s.Category == ScoreCategories.Ragtime).Should().OnlyContain(s => !s.IsSong);

        scores.Count(s => s.IsSong).Should().Be(35);
    }
}
