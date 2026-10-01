using FluentAssertions;

using XyloType.Application.Services;
using XyloType.Domain.Music;
using XyloType.ViewModels.Typing;

namespace XyloType.Tests.ViewModels;

public class ScoreCategoryViewModelTests
{
    private static ScoreOptionViewModel Piece(string id, bool enabled, List<string> changes)
        => new(new Score(id, id, "Me", [72]), enabled, option => changes.Add($"{option.Score.Id}:{option.IsEnabled}"));

    [Fact]
    public void CategorySwitch_AppliesToEveryPiece()
    {
        List<string> changes = [];
        ScoreOptionViewModel a = Piece("a", true, changes);
        ScoreOptionViewModel b = Piece("b", false, changes);
        ScoreCategoryViewModel category = new("Classique", [a, b]);

        category.CountText.Should().Be("1/2");
        category.IsEnabled.Should().BeTrue("at least one piece is played");

        category.IsEnabled = false;
        category.CountText.Should().Be("0/2");
        changes.Should().Equal("a:False");

        category.IsEnabled = true;
        category.CountText.Should().Be("2/2");
        a.IsEnabled.Should().BeTrue();
        b.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void PieceSwitch_UpdatesTheCategoryCount()
    {
        List<string> changes = [];
        ScoreOptionViewModel a = Piece("a", true, changes);
        ScoreCategoryViewModel category = new("Noël", [a]);

        a.IsEnabled = false;

        category.CountText.Should().Be("0/1");
        category.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Catalog_EveryCategoryIsDisplayed()
    {
        IReadOnlyList<Score> scores = new ScoreCatalog().GetAll();

        scores.Select(s => s.Category).Distinct()
            .Should().BeSubsetOf(ScoreCategories.DisplayOrder);
        scores.Should().HaveCountGreaterThanOrEqualTo(20);
    }
}
