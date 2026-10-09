using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.Services;
using XyloType.Application.Validators;

namespace XyloType.Tests.Application;

public class ExercisePackTests
{
    private static readonly KeyBoardLayoutDto s_azerty = new(KeyboardLayoutEnumDto.AzertyFr, "Azerty");

    /// <summary>
    /// The packs published from the repository (packs/exercises).
    /// </summary>
    public static TheoryData<string> PackFiles()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "packs", "exercises")))
            dir = dir.Parent;

        TheoryData<string> files = [];
        foreach (string file in Directory.GetFiles(Path.Combine(dir!.FullName, "packs", "exercises"), "*.json"))
            files.Add(file);
        return files;
    }

    private static ExercisePackImporter CreateImporter(ITypingExercicesStorage storage)
    {
        IEditorSplitCharProvider split = Substitute.For<IEditorSplitCharProvider>();
        split.GetSplitCharacter().Returns('\r');
        IGuidProvider guids = Substitute.For<IGuidProvider>();
        guids.CreateGuid().Returns(_ => Guid.NewGuid());

        return new ExercisePackImporter(storage, split, new TypingExerciceValidator(), guids, NullLogger<ExercisePackImporter>.Instance);
    }

    private static ExercisePack Pack(string version, params string[] exerciseKeys)
        => new(1, "test-pack", "Test", "Débutant", "", "AzertyFr", version,
            [new ExercisePackSection("s", "Section", [.. exerciseKeys.Select(k => new ExercisePackExercise(k, k, "", Text: ["abc def"]))])]);

    [Theory]
    [MemberData(nameof(PackFiles))]
    public void RepositoryPack_IsValid(string file)
    {
        ExercisePack? pack = ExercisePack.FromJson(File.ReadAllText(file));

        pack.Should().NotBeNull();
        pack!.Id.Should().Be(Path.GetFileNameWithoutExtension(file), "the file is named after the pack");
        pack.Format.Should().BeLessThanOrEqualTo(ExercisePackCatalog.CurrentFormat);
        pack.Sections.Select(s => s.Key).Should().OnlyHaveUniqueItems();
        pack.Sections.SelectMany(s => s.Exercises).Select(e => e.Key).Should().OnlyHaveUniqueItems();

        var converted = CreateImporter(Substitute.For<ITypingExercicesStorage>()).Convert(pack);
        converted.Success.Should().BeTrue(converted.Error);
        converted.GetValue.Sum(s => s.Exercises.Count).Should().Be(pack.ExerciseCount);
    }

    [Fact]
    public void Ids_AreStable_AndDifferPerPackAndKey()
    {
        ExercisePackIds.Exercise("p", "a").Should().Be(ExercisePackIds.Exercise("p", "a"));
        ExercisePackIds.Exercise("p", "a").Should().NotBe(ExercisePackIds.Exercise("p", "b"));
        ExercisePackIds.Exercise("p", "a").Should().NotBe(ExercisePackIds.Exercise("q", "a"));
        ExercisePackIds.Section("p", "a").Should().NotBe(ExercisePackIds.Exercise("p", "a"));
    }

    [Fact]
    public async Task Import_PutsThePackBeforeTheExercisesOfTheUser_ThenUpdatesItInPlace()
    {
        TypingExercices? saved = null;
        Guid ownSection = Guid.NewGuid();
        TypingExercise own = new() { Id = Guid.NewGuid(), Name = "mine", SectionId = ownSection, AllowedCharacters = "a", TextDataType = new TypingTextDataStatic { GeneratedText = "a" } };

        ITypingExercicesStorage storage = Substitute.For<ITypingExercicesStorage>();
        storage.LoadAsync(Arg.Any<KeyboardLayoutEnumDto>()).Returns(_ => saved is null
            ? Result<TypingExercices>.Ok(new TypingExercices
            {
                KeyboardLayout = s_azerty,
                Sections = [new ExerciseSection { Id = ownSection, Title = "Mine" }],
                Exercices = [own],
            })
            : Result<TypingExercices>.Ok(saved));
        storage.SaveAsync(Arg.Do<TypingExercices>(x => saved = x)).Returns(Result<bool>.Ok(true));
        ExercisePackImporter importer = CreateImporter(storage);

        Result<ExercisePackImportSummary> first = await importer.ImportAsync(Pack("1", "a", "b"), s_azerty);
        Result<ExercisePackImportSummary> update = await importer.ImportAsync(Pack("2", "a", "b", "c"), s_azerty);

        first.GetValue.Should().Be(new ExercisePackImportSummary(2, 0));
        update.GetValue.Should().Be(new ExercisePackImportSummary(1, 2));
        saved!.Exercices.Select(e => e.Name).Should().Equal("a", "b", "c", "mine");
        saved.Sections.Should().HaveCount(2);
        (await importer.GetImportedVersionsAsync(s_azerty)).Should().ContainKey("test-pack").WhoseValue.Should().Be("2");
    }

    [Fact]
    public async Task Import_ForAnotherKeyboard_IsRefused()
    {
        ExercisePackImporter importer = CreateImporter(Substitute.For<ITypingExercicesStorage>());

        Result<ExercisePackImportSummary> result = await importer.ImportAsync(Pack("1", "a"), new KeyBoardLayoutDto(KeyboardLayoutEnumDto.Bepo, "Bépo"));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Exercise_WithNeitherTextNorGenerator_IsRefused()
    {
        ExercisePack pack = new(1, "p", "P", "", "", "AzertyFr", "1",
            [new ExercisePackSection("s", "S", [new ExercisePackExercise("e", "E", "")])]);

        CreateImporter(Substitute.For<ITypingExercicesStorage>()).Convert(pack).Success.Should().BeFalse();
    }
}
