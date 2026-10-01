using FluentAssertions;

using XyloType.Domain.Text;

namespace XyloType.Tests.Domain.Text;

public class WordTokenizerTest
{
    [Theory]
    // compound words stay whole
    [InlineData("peut-être", new[] { "peut-être" })]
    [InlineData("un arc-en-ciel", new[] { "un", "arc-en-ciel" })]
    // elided article / pronoun is dropped
    [InlineData("l'école", new[] { "école" })]
    [InlineData("J'aime qu'il vienne", new[] { "aime", "il", "vienne" })]
    [InlineData("jusqu'à lorsqu'on", new[] { "à", "on" })]
    [InlineData("l'arc-en-ciel", new[] { "arc-en-ciel" })]
    // apostrophe inside a word is kept
    [InlineData("aujourd'hui", new[] { "aujourd'hui" })]
    [InlineData("d'aujourd'hui", new[] { "aujourd'hui" })]
    [InlineData("presqu'île", new[] { "presqu'île" })]
    // euphonic t
    [InlineData("a-t-il", new[] { "a", "il" })]
    [InlineData("Va-t-elle", new[] { "va", "elle" })]
    // typographic apostrophe and hyphen
    [InlineData("l’école", new[] { "école" })]
    [InlineData("aujourd’hui", new[] { "aujourd'hui" })]
    [InlineData("peut‐être", new[] { "peut-être" })]
    // punctuation and stray signs are not part of words
    [InlineData("« Bonjour », dit-il - 'oui' ?", new[] { "bonjour", "dit-il", "oui" })]
    [InlineData("2 chats et 3 chiens", new[] { "chats", "et", "chiens" })]
    public void French(string text, string[] expected)
    {
        WordTokenizer.Tokenize(text, "fr").Should().Equal(expected);
    }

    [Theory]
    [InlineData("don't stop", new[] { "don't", "stop" })]
    [InlineData("well-known l'example", new[] { "well-known", "l'example" })]
    public void English_KeepsApostrophes(string text, string[] expected)
    {
        WordTokenizer.Tokenize(text, "en").Should().Equal(expected);
    }

    [Theory]
    [InlineData("l'amico dell'anno", new[] { "amico", "anno" })]
    [InlineData("c'è", new[] { "è" })]
    public void Italian_DropsElisions(string text, string[] expected)
    {
        WordTokenizer.Tokenize(text, "it").Should().Equal(expected);
    }
}
