using FluentAssertions;

using XyloType.Application.Services;
using XyloType.Domain.Music;

namespace XyloType.Tests.Domain.Music;

public class MelodyTest
{
    // xylophone sounds available: C5 to C7
    private const int Lowest = 72;
    private const int Highest = 96;

    [Theory]
    [InlineData("C4", 60)]
    [InlineData("A4", 69)]
    [InlineData("C5", 72)]
    [InlineData("D#5", 75)]
    [InlineData("Eb5", 75)]
    [InlineData("Bb4", 70)]
    [InlineData("C7", 96)]
    public void Note_ToMidi(string note, int midi)
    {
        Notes.ToMidi(note).Should().Be(midi);
    }

    [Fact]
    public void Melody_IsTransposedByOctaves_IntoTheRange()
    {
        // C3 E3 G3 is far below the range: moved up by whole octaves, intervals kept
        Score score = new("test", "Test", "Me", Notes.ParseMelody("C3 E3 G3"));

        Melody melody = new(score, Lowest, Highest);

        melody.Notes.Should().OnlyContain(n => n >= Lowest && n <= Highest);
        melody.Notes[1].Should().Be(melody.Notes[0] + 4);
        melody.Notes[2].Should().Be(melody.Notes[0] + 7);
        (melody.Notes[0] % 12).Should().Be(0, "still a C");
    }

    [Fact]
    public void Melody_Loops_AtTheEnd()
    {
        Score score = new("test", "Test", "Me", Notes.ParseMelody("C6 D6 E6"));
        Melody melody = new(score, Lowest, Highest);

        int[] played = [.. Enumerable.Range(0, 5).Select(_ => melody.NextNote())];

        played.Should().Equal(84, 86, 88, 84, 86);
    }

    [Fact]
    public void Melody_IsAtStart_RightAfterItsLastNote()
    {
        Score score = new("test", "Test", "Me", Notes.ParseMelody("C6 D6"));
        Melody melody = new(score, Lowest, Highest);

        melody.IsAtStart.Should().BeTrue("nothing played yet");

        melody.NextNote();
        melody.IsAtStart.Should().BeFalse();

        melody.NextNote();
        melody.IsAtStart.Should().BeTrue("the score is complete");
    }

    [Fact]
    public void Melody_WiderThanTheRange_IsFoldedNoteByNote()
    {
        Score score = new("test", "Test", "Me", Notes.ParseMelody("C2 C8"));

        Melody melody = new(score, Lowest, Highest);

        melody.Notes.Should().OnlyContain(n => n >= Lowest && n <= Highest && n % 12 == 0);
    }

    [Fact]
    public void Melody_ThatCannotFitByOctaves_ChangesKey_KeepingIntervals()
    {
        // A4 to E6: 19 semitones, no whole octave shift fits C5..C7
        Score score = new("test", "Test", "Me", Notes.ParseMelody("A4 C5 E6"));

        Melody melody = new(score, Lowest, Highest);

        melody.Notes.Should().Equal(72, 75, 91);
    }

    [Fact]
    public void Catalog_ScoresHaveUniqueIds_AndFitTheXylophoneWithoutFolding()
    {
        IReadOnlyList<Score> scores = new ScoreCatalog().GetAll();

        scores.Should().HaveCountGreaterThanOrEqualTo(4);
        scores.Select(s => s.Id).Should().OnlyHaveUniqueItems();

        foreach (Score score in scores)
        {
            Melody melody = new(score, Lowest, Highest);

            // a transposition keeps every interval of the original melody
            int shift = melody.Notes[0] - score.Notes[0];
            melody.Notes.Should().Equal(score.Notes.Select(n => n + shift), $"{score.Title} must fit without folding");
        }
    }
}
