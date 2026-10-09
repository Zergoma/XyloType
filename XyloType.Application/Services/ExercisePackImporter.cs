using FluentValidation;
using FluentValidation.Results;

using Microsoft.Extensions.Logging;

using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models;
using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Domain.Typing;

namespace XyloType.Application.Services;

public class ExercisePackImporter : IExercisePackImporter
{
    private readonly ITypingExercicesStorage _storage;
    private readonly IEditorSplitCharProvider _splitCharProvider;
    private readonly IValidator<TypingExercise> _validator;
    private readonly IGuidProvider _guidProvider;
    private readonly ILogger<ExercisePackImporter> _logger;

    public ExercisePackImporter(
        ITypingExercicesStorage storage,
        IEditorSplitCharProvider splitCharProvider,
        IValidator<TypingExercise> validator,
        IGuidProvider guidProvider,
        ILogger<ExercisePackImporter> logger)
    {
        _storage = storage;
        _splitCharProvider = splitCharProvider;
        _validator = validator;
        _guidProvider = guidProvider;
        _logger = logger;
    }

    public Result<IReadOnlyList<(ExerciseSection Section, IReadOnlyList<TypingExercise> Exercises)>> Convert(ExercisePack pack)
    {
        List<(ExerciseSection, IReadOnlyList<TypingExercise>)> sections = [];
        List<string> errors = [];

        foreach (ExercisePackSection packSection in pack.Sections)
        {
            ExerciseSection section = new()
            {
                Id = ExercisePackIds.Section(pack.Id, packSection.Key),
                Title = packSection.Title,
                PackId = pack.Id,
                PackVersion = pack.Version,
            };

            List<TypingExercise> exercises = [];
            foreach (ExercisePackExercise packExercise in packSection.Exercises)
            {
                Result<TypingExercise> exercise = ToExercise(pack.Id, section.Id, packExercise);
                if (!exercise.Success)
                {
                    errors.Add($"{packExercise.Key} : {exercise.Error}");
                    continue;
                }

                ValidationResult validation = _validator.Validate(exercise.GetValue);
                if (!validation.IsValid)
                {
                    errors.Add($"{packExercise.Key} : {string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))}");
                    continue;
                }

                exercises.Add(exercise.GetValue);
            }

            sections.Add((section, exercises));
        }

        if (errors.Count > 0)
            return Result<IReadOnlyList<(ExerciseSection, IReadOnlyList<TypingExercise>)>>
                .Fail($"Le pack « {pack.Title} » est invalide : {string.Join(" ; ", errors)}");

        return Result<IReadOnlyList<(ExerciseSection, IReadOnlyList<TypingExercise>)>>.Ok(sections);
    }

    private Result<TypingExercise> ToExercise(string packId, Guid sectionId, ExercisePackExercise source)
    {
        TypingExercise exercise = new()
        {
            Id = ExercisePackIds.Exercise(packId, source.Key),
            Name = source.Name,
            Description = source.Description,
            SectionId = sectionId,
        };

        switch (source)
        {
            case { Text: { Count: > 0 } lines, Generator: null }:
                string text = string.Join(_splitCharProvider.GetSplitCharacter(), lines);
                exercise.TextDataType = new TypingTextDataStatic { GeneratedText = text };
                // the letters given first, in their order, then the other ones of the text
                exercise.AllowedCharacters = AllowedLettersExtractor.ExtractAllowedLetters(source.Letters ?? string.Empty, text);
                return Result<TypingExercise>.Ok(exercise);

            case { Text: null or { Count: 0 }, Generator: ExercisePackGenerator generator }:
                GeneratedTypeSource? kind = generator.Source switch
                {
                    ExercisePackGenerator.PseudoWords => GeneratedTypeSource.PseudoWords,
                    ExercisePackGenerator.Words => GeneratedTypeSource.Words,
                    _ => null,
                };
                if (kind is null)
                    return Result<TypingExercise>.Fail($"source inconnue « {generator.Source} »");

                exercise.TextDataType = new TypingTextDataDynamic
                {
                    GeneratedTypeSource = kind.Value,
                    LengthMin = generator.LengthMin,
                    LengthMax = generator.LengthMax,
                    LanguagesSelected = [.. generator.Languages ?? []],
                };
                exercise.AllowedCharacters = source.Letters ?? string.Empty;
                return Result<TypingExercise>.Ok(exercise);

            default:
                return Result<TypingExercise>.Fail("un texte ou un générateur, l'un des deux");
        }
    }

    public async Task<Result<ExercisePackImportSummary>> ImportAsync(ExercisePack pack, KeyBoardLayoutDto keyboard)
    {
        if (!string.Equals(pack.Layout, keyboard.KeyBoardCode.ToString(), StringComparison.OrdinalIgnoreCase))
            return Result<ExercisePackImportSummary>.Fail($"Ce pack est fait pour un autre clavier ({pack.Layout}).");

        var converted = Convert(pack);
        if (!converted.Success)
            return Result<ExercisePackImportSummary>.Fail(converted.Error);

        Result<TypingExercices> loaded = await _storage.LoadAsync(keyboard.KeyBoardCode);
        TypingExercices exercises = loaded.Success ? loaded.GetValue : new TypingExercices { KeyboardLayout = keyboard };

        int added = 0, updated = 0;
        foreach ((ExerciseSection section, IReadOnlyList<TypingExercise> packExercises) in converted.GetValue)
        {
            // the section: its title and version follow the pack, its place stays the one chosen by the user
            if (exercises.Sections.Find(s => s.Id == section.Id) is ExerciseSection existing)
            {
                existing.Title = section.Title;
                existing.PackId = section.PackId;
                existing.PackVersion = section.PackVersion;
            }
            else
            {
                // after the sections of the packs, before the ones of the user: the progression comes first
                exercises.Sections.Insert(exercises.Sections.FindLastIndex(s => s.PackId is not null) + 1, section);
            }

            foreach (TypingExercise exercise in packExercises)
            {
                int index = exercises.Exercices.FindIndex(e => e.Id == exercise.Id);
                if (index >= 0)
                {
                    exercises.Exercices[index] = exercise;
                    updated++;
                }
                else
                {
                    exercises.Exercices.Add(exercise);
                    added++;
                }
            }
        }

        exercises.NormalizeSections(_guidProvider.CreateGuid);

        Result<bool> saved = await _storage.SaveAsync(exercises);
        if (!saved.Success)
            return Result<ExercisePackImportSummary>.Fail(saved.Error);

        _logger.LogInformation(
            "Exercise pack {PackId} {Version} imported for {Keyboard}: {Added} added, {Updated} updated",
            pack.Id,
            pack.Version,
            keyboard.KeyBoardCode,
            added,
            updated);

        return Result<ExercisePackImportSummary>.Ok(new ExercisePackImportSummary(added, updated));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetImportedVersionsAsync(KeyBoardLayoutDto keyboard)
    {
        Result<TypingExercices> loaded = await _storage.LoadAsync(keyboard.KeyBoardCode);
        if (!loaded.Success)
            return new Dictionary<string, string>();

        return loaded.GetValue.Sections
            .Where(s => s.PackId is not null && s.PackVersion is not null)
            .GroupBy(s => s.PackId!)
            .ToDictionary(g => g.Key, g => g.Select(s => s.PackVersion!).Min(StringComparer.Ordinal)!);
    }
}
