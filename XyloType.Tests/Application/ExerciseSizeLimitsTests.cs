using FluentAssertions;

using NSubstitute;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Application.Services;

namespace XyloType.Tests.Application;

public class ExerciseSizeLimitsTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(ExerciseSizeLimits.MaxLines, true)]
    [InlineData(ExerciseSizeLimits.MaxLines + 1, false)]
    public void LineNumber_IsSavedOnlyWithinTheLimits(int lines, bool saved)
    {
        IUserTypingPreferenceService preference = Substitute.For<IUserTypingPreferenceService>();

        var result = new TypingExerciseLineNumberService(preference).SetLineNumber(lines);

        result.Success.Should().Be(saved);
        preference.Received(saved ? 1 : 0).SetLineNumber(lines);
        if (!saved)
            result.Error.Should().Contain($"entre 1 et {ExerciseSizeLimits.MaxLines}");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(ExerciseSizeLimits.MaxWordsPerLine, true)]
    [InlineData(ExerciseSizeLimits.MaxWordsPerLine + 1, false)]
    public void WordsPerLine_IsSavedOnlyWithinTheLimits(int words, bool saved)
    {
        IUserTypingPreferenceService preference = Substitute.For<IUserTypingPreferenceService>();

        var result = new TypingExerciseWordNumberService(preference).SetItemNumber(words);

        result.Success.Should().Be(saved);
        preference.Received(saved ? 1 : 0).SetWordNumber(words);
    }

    [Fact]
    public void ValuesSavedBeforeTheLimits_AreBroughtBackWithinThem()
    {
        IUserTypingPreferenceService preference = Substitute.For<IUserTypingPreferenceService>();
        preference.GetLineNumber().Returns(500);
        preference.GetWordNumber().Returns(80);

        new TypingExerciseLineNumberService(preference).LineNumber.Should().Be(ExerciseSizeLimits.MaxLines);
        new TypingExerciseWordNumberService(preference).ItemNumber.Should().Be(ExerciseSizeLimits.MaxWordsPerLine);
    }

    [Fact]
    public void FitLines_KeepsWholeLinesWithinTheTextLimit()
    {
        string line = new('a', 999);    // 4 lines and 3 separators: 3999 characters
        string[] lines = [line, line, line, line, line];

        string[] kept = [.. ExerciseSizeLimits.FitLines(lines, "\r")];

        kept.Should().HaveCount(4);
        string.Join("\r", kept).Length.Should().BeLessThanOrEqualTo(ExerciseSizeLimits.MaxTextLength);
    }

    [Fact]
    public void FitLines_KeepsAShortTextAsIs()
        => ExerciseSizeLimits.FitLines(["le chat", "dort"], "\r").Should().Equal("le chat", "dort");

    [Theory]
    [InlineData(ExerciseSizeLimits.MaxTextLength, true)]
    [InlineData(ExerciseSizeLimits.MaxTextLength + 1, false)]
    public void FixedText_IsSavedOnlyWithinTheLimit(int length, bool valid)
    {
        var result = new XyloType.Application.Validators.TypingTextDataStaticValidator()
            .Validate(new XyloType.Application.Models.Typing.Exercices.TypingTextDataStatic { GeneratedText = new string('a', length) });

        result.IsValid.Should().Be(valid);
        if (!valid)
            result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain("raccourcissez-le");
    }

    [Fact]
    public void NothingSaved_GivesTheDefaults()
    {
        IUserTypingPreferenceService preference = Substitute.For<IUserTypingPreferenceService>();

        new TypingExerciseLineNumberService(preference).LineNumber.Should().Be(IUserTypingPreferenceService.DefaultLineNumber);
        new TypingExerciseWordNumberService(preference).ItemNumber.Should().Be(IUserTypingPreferenceService.DefaultWordNumber);
    }
}
