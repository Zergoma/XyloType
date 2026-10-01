using FluentAssertions;

using XyloType.Domain.Text;

namespace XyloType.Tests.Domain.Text;

public class TitleSimilarityTest
{
    [Theory]
    [InlineData("Les Misérables", "les miserables")]
    [InlineData("Les Misérables", "Les_Misérables (1)")]
    [InlineData("Les Misérables", "les-miserables-copie")]
    [InlineData("Germinal v2", "germinal")]
    [InlineData("Victor Hugo - Les Misérables - Tome 1", "Les Misérables")]
    [InlineData("Le Comte de Monte-Cristo", "Le Comte de Monte Christo")]
    public void SameBook_IsClose(string title, string other)
    {
        TitleSimilarity.AreClose(title, other).Should().BeTrue();
    }

    [Theory]
    [InlineData("Les Misérables", "Germinal")]
    [InlineData("Le Rouge et le Noir", "Le Père Goriot")]
    [InlineData("1984", "2001")]
    public void DifferentBooks_AreNotClose(string title, string other)
    {
        TitleSimilarity.AreClose(title, other).Should().BeFalse();
    }

    [Fact]
    public void Normalize_RemovesAccentsPunctuationAndCopyMarkers()
    {
        TitleSimilarity.Normalize("Les_Misérables (2) - copie v3").Should().Be("les miserables");
    }
}
