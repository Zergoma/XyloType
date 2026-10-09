using FluentAssertions;

using XyloType.Application.Models;
using XyloType.Application.Models.Themes;
using XyloType.Application.Services;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Tests.Application;

public class TypingLevelTests
{
    [Theory]
    [InlineData("Débutant", TypingLevel.Beginner)]
    [InlineData("intermédiaire", TypingLevel.Intermediate)]
    [InlineData("Confirmé", TypingLevel.Expert)]
    [InlineData("expert", TypingLevel.Expert)]
    [InlineData("", TypingLevel.Intermediate)]
    [InlineData(null, TypingLevel.Intermediate)]
    public void LevelNames_AreParsed(string? name, TypingLevel level)
        => TypingLevelNames.Parse(name).Should().Be(level);

    [Fact]
    public void LevelNames_RoundTrip()
        => TypingLevelNames.All.Should().AllSatisfy(level => TypingLevelNames.Parse(TypingLevelNames.Of(level)).Should().Be(level));

    [Fact]
    public void Targets_AreStricterAtAHigherLevel()
    {
        TypingTargets beginner = TypingTargets.For(TypingLevel.Beginner);
        TypingTargets intermediate = TypingTargets.For(TypingLevel.Intermediate);
        TypingTargets expert = TypingTargets.For(TypingLevel.Expert);

        // 0.5 s a key: good for a beginner, slow for an expert
        beginner.ResponseBadness(0.5).Should().Be(0);
        intermediate.ResponseBadness(0.5).Should().BeInRange(0.01, 0.99);
        expert.ResponseBadness(0.5).Should().Be(1);

        beginner.ErrorBadness(8).Should().Be(0);
        expert.ErrorBadness(8).Should().BeGreaterThan(0.5);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, 1)]
    [InlineData(double.NaN, 0)]
    public void ColorScale_StaysWithinItsEnds(double badness, double clamped)
    {
        StatColorScale scale = new();

        scale.GetHexColor(badness, ThemeState.Light).Should().Be(scale.GetHexColor(clamped, ThemeState.Light));
    }

    [Fact]
    public void ColorScale_GoesFromGreenToRed()
    {
        StatColorScale scale = new();

        (int r, int g, _) = Rgb(scale.GetHexColor(0, ThemeState.Dark));
        g.Should().BeGreaterThan(r, "good is green");

        (r, g, _) = Rgb(scale.GetHexColor(1, ThemeState.Dark));
        r.Should().BeGreaterThan(g, "bad is red");

        scale.GetHexColor(0.3, ThemeState.Light).Should().MatchRegex("^#[0-9A-F]{6}$");
    }

    private static (int R, int G, int B) Rgb(string hex)
        => (Convert.ToInt32(hex[1..3], 16), Convert.ToInt32(hex[3..5], 16), Convert.ToInt32(hex[5..7], 16));
}
