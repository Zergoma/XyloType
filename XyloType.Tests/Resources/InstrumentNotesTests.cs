using FluentAssertions;

namespace XyloType.Tests.Resources;

/// <summary>
/// Every instrument must have a sound for every playable note, otherwise a note stays silent.
/// </summary>
public class InstrumentNotesTests
{
    // range of the melodies (see TypingViewModel): C5 to C7
    private const int LowestNote = 72;
    private const int HighestNote = 96;

    [Theory]
    [InlineData("xylophone", "xylo")]
    [InlineData("piano", "piano")]
    [InlineData("xylophone2", "xylo2")]
    [InlineData("marimba", "marimba")]
    [InlineData("vibraphone", "vibraphone")]
    [InlineData("glockenspiel", "glock")]
    public void EveryNote_HasASoundFile(string folder, string prefix)
    {
        string raw = Path.Combine(RepositoryRoot(), "XyloType", "Resources", "Raw", folder);

        IEnumerable<string> missing = Enumerable
            .Range(LowestNote, HighestNote - LowestNote + 1)
            .Select(midi => $"{prefix}_{midi}.wav")
            .Where(file => !File.Exists(Path.Combine(raw, file)));

        missing.Should().BeEmpty();
    }

    [Theory]
    [InlineData("piano", "piano")]
    [InlineData("xylophone2", "xylo2")]
    [InlineData("marimba", "marimba")]
    [InlineData("vibraphone", "vibraphone")]
    [InlineData("glockenspiel", "glock")]
    public void RecordedInstruments_HaveAnErrorSound(string folder, string prefix)
    {
        string file = Path.Combine(RepositoryRoot(), "XyloType", "Resources", "Raw", folder, $"{prefix}_error.wav");

        File.Exists(file).Should().BeTrue();
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "XyloType.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
    }
}
