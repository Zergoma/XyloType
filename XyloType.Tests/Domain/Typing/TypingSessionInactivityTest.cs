using FluentAssertions;

using XyloType.Domain.Typing;

namespace XyloType.Tests.Domain.Typing;

public class TypingSessionInactivityTest
{
    // short delays instead of 5 s and 1 s: the rule is the same
    private static TypingSession CreateSession()
    {
        TypingSession session = new()
        {
            InactivityLimit = TimeSpan.FromMilliseconds(100),
            InactivityKept = TimeSpan.FromMilliseconds(20),
        };
        session.Lines.Add(new TypingLine("abc"));
        session.ResetProgression();

        // the exercise is shown: the typing area gets the focus
        session.Resume();
        return session;
    }

    [Fact]
    public void AtTheStart_NoKeyForTheLimit_Pauses_Once()
    {
        TypingSession session = CreateSession();
        Thread.Sleep(150);

        session.PauseIfInactive().Should().BeTrue();
        session.Duration.Should().Be(TimeSpan.Zero);

        // no new pause until the user comes back
        Thread.Sleep(150);
        session.PauseIfInactive().Should().BeFalse();
    }

    [Fact]
    public void WhileTyping_NothingPauses()
    {
        TypingSession session = CreateSession();
        session.ProcessInput('a', c => c);
        Thread.Sleep(30);

        session.PauseIfInactive().Should().BeFalse();
    }

    [Fact]
    public void NoKeyForTheLimit_Pauses_AndKeepsOnlyALittleOfTheIdleTime()
    {
        TypingSession session = CreateSession();
        session.ProcessInput('a', c => c);
        Thread.Sleep(150);

        session.PauseIfInactive().Should().BeTrue();

        // about 150 ms idle: only the 20 ms kept count
        session.Duration.Should().BeLessThan(TimeSpan.FromMilliseconds(80));

        // paused: the clock no longer runs, and it does not pause twice
        TimeSpan paused = session.Duration;
        Thread.Sleep(50);
        session.Duration.Should().Be(paused);
        session.PauseIfInactive().Should().BeFalse();
    }

    [Fact]
    public void BackToTheExercise_DoesNotPauseAgainRightAway()
    {
        TypingSession session = CreateSession();
        session.ProcessInput('a', c => c);
        Thread.Sleep(150);
        session.PauseIfInactive().Should().BeTrue();

        // "Reprendre la saisie": the focus comes back
        session.Resume();

        session.PauseIfInactive().Should().BeFalse();
        Thread.Sleep(150);
        session.PauseIfInactive().Should().BeTrue();
    }

    [Fact]
    public void TheIdleTime_IsAlsoTakenOffTheResponseTimeOfTheLetter()
    {
        TypingSession session = CreateSession();
        session.ProcessInput('a', c => c);
        Thread.Sleep(150);
        session.PauseIfInactive();

        // back to typing: the "b" did not take the whole idle time
        session.ProcessInput('b', c => c);

        session.Lines[0].Characters[1].RespondeTime.Should().BeLessThan(TimeSpan.FromMilliseconds(80));
    }
}
