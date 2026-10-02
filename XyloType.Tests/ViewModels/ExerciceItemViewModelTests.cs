using FluentAssertions;

using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.ViewModels.TypingLauncher;

namespace XyloType.Tests.ViewModels;

public class ExerciceItemViewModelTests
{
    private static ExerciceItemViewModel Create(TypingTextData data)
        => new(new TypingExercise { Id = Guid.NewGuid(), Name = "test", TextDataType = data }, 0);

    [Fact]
    public void Badge_TellsInventedWordsFromRealWords()
    {
        ExerciceItemViewModel pseudo = Create(new TypingTextDataDynamic { GeneratedTypeSource = GeneratedTypeSource.PseudoWords });
        ExerciceItemViewModel real = Create(new TypingTextDataDynamic { GeneratedTypeSource = GeneratedTypeSource.Words });
        ExerciceItemViewModel fixedText = Create(new TypingTextDataStatic());

        pseudo.IsPseudoWords.Should().BeTrue();
        pseudo.BadgeText.Should().Be("Mots inventés");

        real.IsRealWords.Should().BeTrue();
        real.BadgeText.Should().Be("Vrais mots");

        fixedText.IsDynamic.Should().BeFalse();
        fixedText.BadgeText.Should().BeEmpty();
    }

    [Fact]
    public void FixedText_PreviewShowsTheFirstLines_AndTellsWhenTruncated()
    {
        string longText = string.Join("\r\n", Enumerable.Range(1, 25).Select(i => $"ligne {i}"));
        ExerciceItemViewModel item = Create(new TypingTextDataStatic { GeneratedText = longText });

        item.HasTextPreview.Should().BeTrue();
        item.TextPreview.Split('\n').Should().HaveCount(20).And.StartWith("ligne 1").And.EndWith("ligne 20");
        item.IsTextPreviewTruncated.Should().BeTrue();
        item.TextPreviewTruncatedText.Should().Be("… texte tronqué : 20 lignes sur 25");

        ExerciceItemViewModel shortText = Create(new TypingTextDataStatic { GeneratedText = "a b c\nd e f\n" });
        shortText.IsTextPreviewTruncated.Should().BeFalse();
        shortText.TextPreview.Should().Be("a b c\nd e f");

        Create(new TypingTextDataDynamic()).HasTextPreview.Should().BeFalse();
    }
}
