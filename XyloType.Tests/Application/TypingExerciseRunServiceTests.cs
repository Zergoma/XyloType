using FluentAssertions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.Services;

namespace XyloType.Tests.Application;

public class TypingExerciseRunServiceTests
{
    private static readonly KeyBoardLayoutDto s_keyboard = new(KeyboardLayoutEnumDto.AzertyFr, "Azerty");

    private static TypingExercices CreateExercises(int count)
        => new()
        {
            KeyboardLayout = s_keyboard,
            Exercices = [.. Enumerable
                .Range(0, count)
                .Select(i => new TypingExercise { Id = Guid.NewGuid(), Name = $"Exercise {i}" })]
        };

    /// <summary>
    /// Orchestrator returning a provider that generates a different text on each call,
    /// like a dynamic exercise does.
    /// </summary>
    private static ICreateStringProviderOrchestrator CreateOrchestrator()
    {
        ICreateStringProviderOrchestrator orchestrator = Substitute.For<ICreateStringProviderOrchestrator>();
        orchestrator
            .Create(Arg.Any<TypingExercise>(), Arg.Any<KeyBoardLayoutDto>())
            .Returns(call =>
            {
                IStringsProvider provider = Substitute.For<IStringsProvider>();
                provider
                    .GetStringsAsync()
                    .Returns(_ => Result<IEnumerable<string>>.Ok([Guid.NewGuid().ToString()]));
                return Result<IStringsProvider>.Ok(provider);
            });
        return orchestrator;
    }

    [Fact]
    public void Start_SetsCurrentExercise()
    {
        // Arrange
        TypingExercices exercises = CreateExercises(3);
        TypingExerciseRunService service = new(CreateOrchestrator());

        // Act
        Result<IStringsProvider> result = service.Start(exercises, 1, s_keyboard);

        // Assert
        result.Success.Should().BeTrue();
        service.CurrentExerciseId.Should().Be(exercises.Exercices[1].Id);
        service.CurrentExerciseName.Should().Be("Exercise 1");
        service.HasNext.Should().BeTrue();
    }

    [Fact]
    public void Start_OutOfRange_Fails()
    {
        // Arrange
        TypingExerciseRunService service = new(CreateOrchestrator());

        // Act
        Result<IStringsProvider> result = service.Start(CreateExercises(2), 2, s_keyboard);

        // Assert
        result.Success.Should().BeFalse();
        service.CurrentExerciseId.Should().BeNull();
    }

    [Fact]
    public void Next_MovesToFollowingExercise_UntilTheEnd()
    {
        // Arrange
        TypingExercices exercises = CreateExercises(2);
        TypingExerciseRunService service = new(CreateOrchestrator());
        service.Start(exercises, 0, s_keyboard);

        // Act
        Result<IStringsProvider> next = service.Next();
        Result<IStringsProvider> afterLast = service.Next();

        // Assert
        next.Success.Should().BeTrue();
        service.CurrentExerciseId.Should().Be(exercises.Exercices[1].Id);
        service.HasNext.Should().BeFalse();
        afterLast.Success.Should().BeFalse();
        service.CurrentExerciseId.Should().Be(exercises.Exercices[1].Id);
    }

    [Fact]
    public void Retry_RecreatesTheTextOfTheSameExercise()
    {
        // Arrange
        TypingExercices exercises = CreateExercises(2);
        ICreateStringProviderOrchestrator orchestrator = CreateOrchestrator();
        TypingExerciseRunService service = new(orchestrator);
        service.Start(exercises, 0, s_keyboard);

        // Act
        Result<IStringsProvider> retry = service.Retry();

        // Assert
        retry.Success.Should().BeTrue();
        service.CurrentExerciseId.Should().Be(exercises.Exercices[0].Id);
        orchestrator.Received(2).Create(exercises.Exercices[0], s_keyboard);
    }

    [Fact]
    public void Retry_WithoutStart_Fails()
    {
        // Arrange
        TypingExerciseRunService service = new(CreateOrchestrator());

        // Act
        Result<IStringsProvider> retry = service.Retry();

        // Assert
        retry.Success.Should().BeFalse();
    }
}
