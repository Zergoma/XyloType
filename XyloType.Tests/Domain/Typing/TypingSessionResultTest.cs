using FluentAssertions;

using XyloType.Domain.Typing;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Tests.Domain.Typing;

public class TypingSessionResultTest
{
    private static TypingSession CreateSession(params string[] lines)
    {
        TypingSession session = new();
        foreach (string line in lines)
            session.Lines.Add(new TypingLine(line));
        return session;
    }

    [Fact]
    public void RepeatedCharacter_SumsResponseTimesOfEveryOccurrence()
    {
        // Arrange: "a" appears twice on the same line
        TypingSession session = CreateSession("aba");
        List<TypingChar> chars = session.Lines[0].Characters;
        chars[0].RespondeTime = TimeSpan.FromMilliseconds(200);
        chars[1].RespondeTime = TimeSpan.FromMilliseconds(300);
        chars[2].RespondeTime = TimeSpan.FromMilliseconds(400);

        // Act
        TypingSessionResult result = session.GetResult();

        // Assert
        CharStats a = result.CharStats['a'];
        a.NbOccurence.Should().Be(2);
        a.RespondeTime.Should().Be(TimeSpan.FromMilliseconds(600));
        a.ResponseTimeAverage.Should().Be(TimeSpan.FromMilliseconds(300));
        result.CharStats['b'].NbOccurence.Should().Be(1);
    }

    [Fact]
    public void Statistics_AreAggregatedAcrossLines()
    {
        // Arrange
        TypingSession session = CreateSession("ab", "a");
        session.Lines[0].Characters[0].RespondeTime = TimeSpan.FromMilliseconds(100);
        session.Lines[1].Characters[0].RespondeTime = TimeSpan.FromMilliseconds(500);

        // Act
        TypingSessionResult result = session.GetResult();

        // Assert
        result.CharStats['a'].NbOccurence.Should().Be(2);
        result.CharStats['a'].RespondeTime.Should().Be(TimeSpan.FromMilliseconds(600));
    }

    [Fact]
    public void Errors_CountCharactersWithErrorsAndEveryWrongKey()
    {
        // Arrange: first "a" missed twice, second "a" right the first time
        TypingSession session = CreateSession("aa");
        session.Lines[0].Characters[0].Errors.AddRange(['z', 'q']);

        // Act
        TypingSessionResult result = session.GetResult();

        // Assert
        CharStats a = result.CharStats['a'];
        a.NbOccurence.Should().Be(2);
        a.NbCharError.Should().Be(1);
        a.RealErrors.Should().Equal('z', 'q');
    }

    [Fact]
    public void Duration_RunsFromFirstKeyToLastCharacter()
    {
        // Arrange
        TypingSession session = CreateSession("ab");
        session.ResetProgression();

        // Act
        session.ProcessInput('a', c => c);
        Thread.Sleep(50);
        TypingStatus status = session.ProcessInput('b', c => c);
        TimeSpan durationAtEnd = session.GetResult().Duration;
        Thread.Sleep(50);

        // Assert
        status.Should().Be(TypingStatus.Ended);
        durationAtEnd.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40));
        session.GetResult().Duration.Should().Be(durationAtEnd, "the clock stops at the last character");
    }

    [Fact]
    public void Pause_StopsTheClock_UntilResume()
    {
        // Arrange
        TypingSession session = CreateSession("abc");
        session.ResetProgression();
        session.ProcessInput('a', c => c);

        // Act
        session.Pause();
        TimeSpan atPause = session.Duration;
        Thread.Sleep(80);
        TimeSpan afterPause = session.Duration;

        session.Resume();
        Thread.Sleep(30);

        // Assert
        afterPause.Should().Be(atPause, "no time is counted while paused");
        session.Duration.Should().BeGreaterThan(atPause, "the clock runs again after resume");
    }

    [Fact]
    public void KeyPress_ResumesAPausedSession()
    {
        // Arrange
        TypingSession session = CreateSession("abc");
        session.ResetProgression();
        session.ProcessInput('a', c => c);
        session.Pause();

        // Act
        session.ProcessInput('b', c => c);
        TimeSpan afterKey = session.Duration;
        Thread.Sleep(30);

        // Assert
        session.Duration.Should().BeGreaterThan(afterKey);
    }

    [Fact]
    public void Pause_BeforeFirstKey_DoesNotStartTheClock()
    {
        // Arrange
        TypingSession session = CreateSession("abc");
        session.ResetProgression();

        // Act
        session.Pause();
        session.Resume();
        Thread.Sleep(30);

        // Assert
        session.Duration.Should().Be(TimeSpan.Zero);
    }
}
