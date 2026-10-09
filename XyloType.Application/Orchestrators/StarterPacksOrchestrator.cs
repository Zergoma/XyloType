using Microsoft.Extensions.Logging;

using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Application.Orchestrators;

public class StarterPacksOrchestrator : IStarterPacksOrchestrator
{
    private readonly IExercisePackSource _exerciseSource;
    private readonly IExercisePackImporter _exerciseImporter;
    private readonly IWordPackSource _wordSource;
    private readonly IWordImportOrchestrator _wordImporter;
    private readonly IKeyboardKeyLocatorManager _keyboardLocators;
    private readonly IDactyloRepository _words;
    private readonly ILogger<StarterPacksOrchestrator> _logger;

    public StarterPacksOrchestrator(
        IExercisePackSource exerciseSource,
        IExercisePackImporter exerciseImporter,
        IWordPackSource wordSource,
        IWordImportOrchestrator wordImporter,
        IKeyboardKeyLocatorManager keyboardLocators,
        IDactyloRepository words,
        ILogger<StarterPacksOrchestrator> logger)
    {
        _exerciseSource = exerciseSource;
        _exerciseImporter = exerciseImporter;
        _wordSource = wordSource;
        _wordImporter = wordImporter;
        _keyboardLocators = keyboardLocators;
        _words = words;
        _logger = logger;
    }

    public async Task<StarterPacksOffer> GetOfferAsync(KeyBoardLayoutDto keyboard, CancellationToken cancellationToken = default)
    {
        // the exercise packs of the keyboard not imported yet
        List<ExercisePackInfo> exercisePacks = [];
        Result<ExercisePackCatalog> exerciseCatalog = await _exerciseSource.GetCatalogAsync(cancellationToken);
        if (exerciseCatalog.Success)
        {
            IReadOnlyDictionary<string, string> imported = await _exerciseImporter.GetImportedVersionsAsync(keyboard);
            exercisePacks.AddRange(exerciseCatalog.GetValue.Packs.Where(p =>
                string.Equals(p.Layout, keyboard.KeyBoardCode.ToString(), StringComparison.OrdinalIgnoreCase)
                && !imported.ContainsKey(p.Id)));
        }

        // a word pack of the language of the keyboard, only for an empty dictionary
        WordPackInfo? wordPack = null;
        WordSearchPage anyWord = await _words.SearchPageAsync(
            new WordQueryBuilder().WithExclusion(WordExclusionFilter.All).Build(), WordSort.Default, 0, 1);
        if (anyWord.TotalCount == 0)
        {
            Result<WordPackCatalog> wordCatalog = await _wordSource.GetCatalogAsync(cancellationToken);
            if (wordCatalog.Success)
            {
                string language = KeyboardLanguage.For(keyboard.KeyBoardCode);
                wordPack = wordCatalog.GetValue.Packs.FirstOrDefault(p => p.LanguageCode == language);
            }
        }

        return new StarterPacksOffer(keyboard, wordPack, exercisePacks, exerciseCatalog.Success);
    }

    public async Task<StarterPacksSummary> InstallAsync(
        StarterPacksOffer offer,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<string> errors = [];
        int exercises = 0;
        int words = 0;

        foreach (ExercisePackInfo pack in offer.ExercisePacks)
        {
            progress?.Report($"Exercices « {pack.Title} »…");

            Result<ExercisePack> downloaded = await _exerciseSource.DownloadAsync(pack, cancellationToken);
            Result<ExercisePackImportSummary> imported = downloaded.Success
                ? await _exerciseImporter.ImportAsync(downloaded.GetValue, offer.Keyboard)
                : Result<ExercisePackImportSummary>.Fail(downloaded.Error);

            if (imported.Success)
                exercises += imported.GetValue.Added;
            else
                errors.Add($"{pack.Title} : {imported.Error}");
        }

        if (offer.WordPack is WordPackInfo wordPack)
        {
            Result<int> imported = await InstallWordsAsync(wordPack, offer.Keyboard, progress, cancellationToken);
            if (imported.Success)
                words = imported.GetValue;
            else
                errors.Add($"{wordPack.Title} : {imported.Error}");
        }

        _logger.LogInformation("Starter packs: {Exercises} exercises, {Words} words, {Errors} errors", exercises, words, errors.Count);
        return new StarterPacksSummary(exercises, words, errors);
    }

    private async Task<Result<int>> InstallWordsAsync(
        WordPackInfo pack,
        KeyBoardLayoutDto keyboard,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        Result<IKeyboardKeysLocator> layout = _keyboardLocators.GetKeyBoardKeyLocator(keyboard);
        if (!layout.Success)
            return Result<int>.Fail(layout.Error);

        Progress<double> download = new(fraction => progress?.Report($"Mots : téléchargement {fraction:P0}"));
        Result<string> file = await _wordSource.DownloadAsync(pack, download, cancellationToken);
        if (!file.Success)
            return Result<int>.Fail(file.Error);

        try
        {
            Progress<WordImportProgress> import = new(p => progress?.Report(p.Phase == WordImportPhase.Reading
                ? $"Mots : analyse {p.Fraction:P0}"
                : $"Mots : enregistrement {p.Fraction:P0}"));

            Result<WordImportSummary> summary = await _wordImporter.ImportPackAsync(file.GetValue, pack, layout.GetValue, import, cancellationToken);
            return summary.Success
                ? Result<int>.Ok(summary.GetValue.NewWords)
                : Result<int>.Fail(summary.Error);
        }
        finally
        {
            try { File.Delete(file.GetValue); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
