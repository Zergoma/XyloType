using FluentAssertions;

using XyloType.Domain.Music;
using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Tests.Domain.Typing;

public class TypingProgressAndSpeedTest
{
    [Fact]
    public void Session_Progress_FollowsTheTypedCharacters()
    {
        TypingSession session = new();
        session.Lines.Add(new TypingLine("ab"));
        session.Lines.Add(new TypingLine("cd"));
        session.ResetProgression();

        session.Progress.Should().Be(0);

        session.ProcessInput('a', c => c);
        session.ProcessInput('b', c => c);
        session.Progress.Should().BeApproximately(0.5, 0.001);

        session.ProcessInput('c', c => c);
        session.ProcessInput('d', c => c);
        session.Progress.Should().Be(1);
    }

    [Fact]
    public void SpeedMeter_CountsTheLastSecondsOnly()
    {
        TypingSpeedMeter meter = new(TimeSpan.FromSeconds(10));

        // nothing meaningful at the very start
        meter.Record(TimeSpan.FromSeconds(0.5));
        meter.WordsPerMinute(TimeSpan.FromSeconds(1)).Should().BeNull();

        // 50 keys in the first 10 seconds: 10 words in 10 s = 60 words per minute
        meter.Reset();
        for (int i = 0; i < 50; i++)
            meter.Record(TimeSpan.FromSeconds(i * 0.2));
        meter.WordsPerMinute(TimeSpan.FromSeconds(10)).Should().BeApproximately(60, 0.001);

        // typing stopped: the old keys leave the window, the speed falls
        meter.WordsPerMinute(TimeSpan.FromSeconds(15)).Should().BeApproximately(30, 1.5);
        meter.WordsPerMinute(TimeSpan.FromSeconds(30)).Should().Be(0);
    }

    [Fact]
    public void SpeedMeter_AtTheStart_UsesTheTimeTypedSoFar()
    {
        TypingSpeedMeter meter = new(TimeSpan.FromSeconds(10));
        for (int i = 0; i < 20; i++)
            meter.Record(TimeSpan.FromSeconds(i * 0.2));

        // 20 keys in 4 seconds = 4 words in 4 s = 60 words per minute
        meter.WordsPerMinute(TimeSpan.FromSeconds(4)).Should().BeApproximately(60, 0.001);
    }

    [Fact]
    public void Melody_Progress_IsFullAfterTheLastNote_ThenStartsAgain()
    {
        Melody melody = new(new Score("t", "t", "", [72, 74, 76, 77]), 72, 96);
        melody.Progress.Should().Be(0);

        melody.NextNote();
        melody.Progress.Should().Be(0.25);

        melody.RemainingNotes.Should().Be(3);

        melody.NextNote();
        melody.NextNote();
        melody.NextNote();
        melody.Progress.Should().Be(1);
        melody.RemainingNotes.Should().Be(0, "the next note starts the melody again");

        melody.NextNote();
        melody.Progress.Should().Be(0.25);
    }
}
