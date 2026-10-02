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
}
