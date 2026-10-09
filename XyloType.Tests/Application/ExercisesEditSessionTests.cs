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

    // the saved exercises are all in this section
    private static readonly Guid s_section = Guid.NewGuid();

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
                Sections = [new ExerciseSection { Id = s_section, Title = "Section" }],
                Exercices = [.. saved.Select(e => new TypingExercise
                {
                    Id = e.Id,
                    SectionId = s_section,
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

        Result<bool> result = session.Move(s_section, from, to);

        result.Success.Should().BeTrue();
        Names(session).Should().Equal(expected);
        session.HasChanges.Should().Be(from != to);
    }

    [Fact]
    public async Task Move_OutOfRange_Fails()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));
        await session.OpenAsync(s_keyboard);

        session.Move(s_section, 0, 2).Success.Should().BeFalse();
        Names(session).Should().Equal("A", "B");
    }

    [Fact]
    public async Task CreateNew_And_Remove_ChangeTheListInMemoryOnly()
    {
        ITypingExercicesStorage storage = CreateStorage("A");
        ExercisesEditSession session = CreateSession(storage);
        await session.OpenAsync(s_keyboard);

        TypingExercise created = session.CreateNew("New", s_section).GetValue;
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
        session.CreateNew("Empty", null); // no letters, no text

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
        session.Move(s_section, 1, 0);

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
        session.Move(s_section, 0, 1);
        session.Exercises[0].Name = "Renamed";
        session.MarkChanged();

        await session.DiscardAsync();

        Names(session).Should().Equal("A", "B");
        session.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNew_WithoutSection_GoesToADefaultSection()
    {
        ITypingExercicesStorage storage = Substitute.For<ITypingExercicesStorage>();
        storage.LoadAsync(Arg.Any<KeyboardLayoutEnumDto>()).Returns(Result<TypingExercices>.Fail("not found"));
        ExercisesEditSession session = CreateSession(storage);
        await session.OpenAsync(s_keyboard);

        TypingExercise created = session.CreateNew("New", null).GetValue;

        session.Sections.Should().ContainSingle().Which.Title.Should().Be(ExerciseSection.DefaultTitle);
        created.SectionId.Should().Be(session.Sections[0].Id);
    }

    [Fact]
    public async Task MoveToSection_PutsTheExerciseAtTheEndOfTheOtherSection()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B", "C"));
        await session.OpenAsync(s_keyboard);
        ExerciseSection first = session.AddSection("First").GetValue;
        session.MoveSection(first.Id, -1);
        session.MoveToSection(session.Exercises.Single(e => e.Name == "C").Id, first.Id);
        session.MoveToSection(session.Exercises.Single(e => e.Name == "A").Id, first.Id);

        Names(session).Should().Equal("C", "A", "B");
        session.Sections.Select(s => s.Title).Should().Equal("First", "Section");
    }

    [Fact]
    public async Task MoveSection_MovesItsExercisesWithIt()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));
        await session.OpenAsync(s_keyboard);
        ExerciseSection other = session.AddSection("Other").GetValue;
        session.CreateNew("New", other.Id);

        session.MoveSection(other.Id, -1).Success.Should().BeTrue();

        Names(session).Should().Equal("New", "A", "B");
        session.MoveSection(other.Id, -1).Success.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSection_RemovesItsExercises()
    {
        ExercisesEditSession session = CreateSession(CreateStorage("A", "B"));
        await session.OpenAsync(s_keyboard);
        ExerciseSection other = session.AddSection("Other").GetValue;
        session.CreateNew("New", other.Id);

        session.RemoveSection(s_section).Success.Should().BeTrue();

        Names(session).Should().Equal("New");
        session.Sections.Should().ContainSingle().Which.Id.Should().Be(other.Id);
    }

    [Fact]
    public void NormalizeSections_PutsTheExercisesWithoutSectionInTheDefaultOne_InSectionOrder()
    {
        Guid a = Guid.NewGuid(), b = Guid.NewGuid();
        TypingExercices exercises = new()
        {
            KeyboardLayout = s_keyboard,
            Sections = [new ExerciseSection { Id = a, Title = "A" }, new ExerciseSection { Id = b, Title = "B" }],
            Exercices =
            [
                new TypingExercise { Id = Guid.NewGuid(), Name = "b1", SectionId = b },
                new TypingExercise { Id = Guid.NewGuid(), Name = "old" },
                new TypingExercise { Id = Guid.NewGuid(), Name = "a1", SectionId = a },
                new TypingExercise { Id = Guid.NewGuid(), Name = "b2", SectionId = b },
            ],
        };

        exercises.NormalizeSections(Guid.NewGuid);

        exercises.Sections.Select(s => s.Title).Should().Equal("A", "B", ExerciseSection.DefaultTitle);
        exercises.Exercices.Select(e => e.Name).Should().Equal("a1", "b1", "b2", "old");
    }
}
