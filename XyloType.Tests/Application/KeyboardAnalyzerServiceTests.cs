using FluentAssertions;

using XyloType.Application;
using XyloType.Application.Models;
using XyloType.Application.Services;
using XyloType.Domain.Entities;
using XyloType.Domain.Models;

namespace XyloType.Tests.Application;

public class KeyboardAnalyzerServiceTests
{
    private static readonly Dictionary<char, KeyInfo> s_azerty = AzertKeyLocatorsBuilder.BuildMap();
    private static readonly KeyboardAnalyzerService s_analyzer = new();

    private static UnitTextAnalysis Analyze(string text)
    {
        Result<UnitTextAnalysis> result = s_analyzer.Analyze(text, s_azerty);
        result.Success.Should().BeTrue();
        return result.GetValue;
    }

    [Theory]
    [InlineData('b', KeyboardRow.D, Finger.LeftIndex)]
    [InlineData('n', KeyboardRow.D, Finger.RightIndex)]
    [InlineData('é', KeyboardRow.A, Finger.LeftRing)]
    [InlineData('è', KeyboardRow.A, Finger.RightIndex)]
    [InlineData('ç', KeyboardRow.A, Finger.RightRing)]
    [InlineData('à', KeyboardRow.A, Finger.RightPinky)]
    [InlineData('ù', KeyboardRow.C, Finger.RightPinky)]
    public void AzertyMap_UsesTheStandardFinger(char c, KeyboardRow row, Finger finger)
    {
        s_azerty[c].Row.Should().Be(row);
        s_azerty[c].Finger.Should().Be(finger);
        s_azerty[c].ExtrenalAccent.Should().BeFalse();
    }

    [Fact]
    public void Circumflex_CombinesTheDeadKeyAndTheVowel()
    {
        // ^ (right pinky, row B) then o (right ring, row B)
        KeyInfo o = s_azerty['ô'];

        o.Finger.Should().Be(Finger.RightPinky | Finger.RightRing);
        o.Row.Should().Be(KeyboardRow.B);
        o.ExtrenalAccent.Should().BeTrue();
    }

    [Fact]
    public void Diaeresis_NeedsShiftTheDeadKeyAndTheVowel()
    {
        // Shift (left pinky) + ^ (right pinky, row B) then e (left middle, row B)
        KeyInfo e = s_azerty['ë'];

        e.Finger.Should().Be(Finger.LeftPinky | Finger.RightPinky | Finger.LeftMiddle);
        e.ExtrenalAccent.Should().BeTrue();
    }

    [Fact]
    public void Word_CombinesRowsAndFingersOfAllLetters()
    {
        // b: left index row D, o: right ring row B, n: right index row D
        UnitTextAnalysis analysis = Analyze("bon");

        analysis.RowMask.Should().Be(KeyboardRow.B | KeyboardRow.D);
        analysis.FingerMask.Should().Be(Finger.LeftIndex | Finger.RightRing | Finger.RightIndex);
        analysis.UsesLeftHand.Should().BeTrue();
        analysis.UsesRightHand.Should().BeTrue();
        analysis.ExternalAccent.Should().BeFalse();
    }

    [Fact]
    public void ExternalAccent_IsKept_EvenIfTheAccentIsNotTheLastLetter()
    {
        UnitTextAnalysis analysis = Analyze("fête");

        analysis.ExternalAccent.Should().BeTrue();
    }

    [Fact]
    public void LeftHandOnlyWord_DoesNotUseTheRightHand()
    {
        UnitTextAnalysis analysis = Analyze("caresse");

        analysis.UsesLeftHand.Should().BeTrue();
        analysis.UsesRightHand.Should().BeFalse();
    }

    [Fact]
    public void DeadKeyOnTheRight_MakesALeftHandWordUseBothHands()
    {
        // "fête": f, t, e are left hand, but ^ is typed with the right pinky
        UnitTextAnalysis analysis = Analyze("fête");

        analysis.UsesLeftHand.Should().BeTrue();
        analysis.UsesRightHand.Should().BeTrue();
    }

    [Theory]
    [InlineData("noël")]
    [InlineData("maïs")]
    [InlineData("été")]
    [InlineData("garçon")]
    public void FrenchWords_CanBeTyped(string word)
    {
        s_analyzer.Analyze(word, s_azerty).Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("cœur")]   // œ is not on a standard AZERTY keyboard
    [InlineData("straße")]
    public void CharactersMissingFromTheLayout_AreRejected(string word)
    {
        s_analyzer.Analyze(word, s_azerty).Success.Should().BeFalse();
    }
}
