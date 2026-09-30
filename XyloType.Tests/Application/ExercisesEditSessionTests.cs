using FluentAssertions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.Services;
using XyloType.Application.Validators;

namespace XyloType.Tests.Application;

public class ExercisesEditSessionTests
{
    private static readonly KeyBoardLayoutDto s_keyboard = new(KeyboardLayoutEnumDto.AzertyFr, "Azerty");

    private static TypingExercise ValidExercise(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            AllowedCharacters = "abc",
            TextDataType = new TypingTextDataStatic { GeneratedText = "abc cab" }
        };

    /// <summary>
    /// Storage returning a fresh copy of the saved exercises on each load, like the real file.
    /// </summary>
    private static ITypingExercicesStorage CreateStorage(params string[] names)
    {
        List<TypingExercise> saved = [.. names.Select(ValidExercise)];

        ITypingExercicesStorage storage = Substitute.For<ITypingExercicesStorage>();
        storage
            .LoadAsync(Arg.Any<KeyboardLayoutEnumDto>())
            .Returns(_ => Result<TypingExercices>.Ok(new TypingExercices
            {
                KeyboardLayout = s_keyboard,
                Exercices = [.. saved.Select(e => new TypingExercise
                {
                    Id = e.Id,
                    Name = e.Name,
                    AllowedCharacters = e.AllowedCharacters,
                    TextDataType = e.TextDataType
                })]
            }));
        storage
            .SaveAsync(Arg.Any<TypingExercices>())
            .Returns(Result<bool>.Ok(true));
        return storage;
    }

    private static ExercisesEditSession CreateSession(ITypingExercicesStorage storage)
    {
        IGuidProvider guidProvider = Substitute.For<IGuidProvider>();
        guidProvider.CreateGuid().Returns(_ => Guid.NewGuid());
        return new ExercisesEditSession(storage, new TypingExerciceValidator(), guidProvider);
    }

    private static string[] Names(ExercisesEditSession session)
        => [.. session.Exercises.Select(e => e.Name)];

    [Fact]
    public async Task Open_LoadsExercises_WithoutChanges()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));

        await session.OpenAsync(s_keyboard);

        Names(session).Should().Equal("A", "B");
        session.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Open_WithoutSavedFile_StartsEmpty()
    {
        ITypingExercicesStorage storage = Substitute.For<ITypingExercicesStorage>();
        storage.LoadAsync(Arg.Any<KeyboardLayoutEnumDto>()).Returns(Result<TypingExercices>.Fail("not found"));
        ExercisesEditSession session = CreateSession(storage);

        Result<bool> result = await session.OpenAsync(s_keyboard);

        result.Success.Should().BeTrue();
        session.Exercises.Should().BeEmpty();
        session.Keyboard.Should().Be(s_keyboard);
    }

    [Theory]
    [InlineData(0, 2, new[] { "B", "C", "A" })]
    [InlineData(2, 0, new[] { "C", "A", "B" })]
    [InlineData(1, 1, new[] { "A", "B", "C" })]
    public async Task Move_ReordersExercises(int from, int to, string[] expected)
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B", "C"));
        await session.OpenAsync(s_keyboard);

        Result<bool> result = session.Move(from, to);

        result.Success.Should().BeTrue();
        Names(session).Should().Equal(expected);
        session.HasChanges.Should().Be(from != to);
    }

    [Fact]
    public async Task Move_OutOfRange_Fails()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));
        await session.OpenAsync(s_keyboard);

        session.Move(0, 2).Success.Should().BeFalse();
        Names(session).Should().Equal("A", "B");
    }

    [Fact]
    public async Task CreateNew_And_Remove_ChangeTheListInMemoryOnly()
    {
        ITypingExercicesStorage storage = CreateStorage("A");
        ExercisesEditSession session = CreateSession(storage);
        await session.OpenAsync(s_keyboard);

        TypingExercise created = session.CreateNew("New").GetValue;
        session.Remove(session.Exercises[0].Id);

        Names(session).Should().Equal("New");
        session.Exercises[0].Should().BeSameAs(created);
        session.HasChanges.Should().BeTrue();
        await storage.DidNotReceive().SaveAsync(Arg.Any<TypingExercices>());
    }

    [Fact]
    public async Task Save_WithInvalidExercise_FailsAndWritesNothing()
    {
        ITypingExercicesStorage storage = CreateStorage("A");
        ExercisesEditSession session = CreateSession(storage);
        await session.OpenAsync(s_keyboard);
        session.CreateNew("Empty"); // no letters, no text

        Result<bool> result = await session.SaveAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Empty");
        session.HasChanges.Should().BeTrue();
        await storage.DidNotReceive().SaveAsync(Arg.Any<TypingExercices>());
    }

    [Fact]
    public async Task Save_WritesTheWholeListInItsNewOrder()
    {
        ITypingExercicesStorage storage = CreateStorage("A", "B");
        ExercisesEditSession session = CreateSession(storage);
        await session.OpenAsync(s_keyboard);
        session.Move(1, 0);

        Result<bool> result = await session.SaveAsync();

        result.Success.Should().BeTrue();
        session.HasChanges.Should().BeFalse();
        await storage.Received(1).SaveAsync(Arg.Is<TypingExercices>(x =>
            x.Exercices.Select(e => e.Name).SequenceEqual(new[] { "B", "A" })));
    }

    [Fact]
    public async Task Discard_ReloadsTheSavedExercises()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));
        await session.OpenAsync(s_keyboard);
        session.Move(0, 1);
        session.Exercises[0].Name = "Renamed";
        session.MarkChanged();

        await session.DiscardAsync();

        Names(session).Should().Equal("A", "B");
        session.HasChanges.Should().BeFalse();
    }
}
