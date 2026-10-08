using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Application.ValueObjects;
using XyloType.Domain.Entities;

namespace XyloType.Application.Orchestrators;

public class WordImportOrchestrator : IWordImportOrchestrator
{
    private readonly IDactyloRepository _repository;
    private readonly IWordBatchProcessorOrchestrator _wordBatchProcessorOrchestrator;
    private readonly IWordStreamReader _wordStreamingService;
    private readonly IWordPackReader _packReader;
    private readonly IContentHasher _hasher;
    private readonly ILogger<WordImportOrchestrator> _logger;

    // words are analyzed by batches; everything is written once at the end
    private const int BatchSize = 2000;

    // ignored words shown to the user
    private const int IgnoredSampleSize = 20;

    public WordImportOrchestrator(
        IDactyloRepository dactyloRepository,
        IWordBatchProcessorOrchestrator processor,
        IWordStreamReader wordStreamingService,
        IWordPackReader packReader,
        IContentHasher hasher,
        ILogger<WordImportOrchestrator> logger)
    {
        _repository = dactyloRepository;
        _wordBatchProcessorOrchestrator = processor;
        _wordStreamingService = wordStreamingService;
        _packReader = packReader;
        _hasher = hasher;
        _logger = logger;
    }

    public Task<Result<WordImportSummary>> ImportAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => RunAsync(
            filePath,
            ct => ImportCoreAsync(
                // each word of the text counts once
                fileProgress => CountOnce(_wordStreamingService.ReadWordsAsync(filePath, languageCode, fileProgress, ct)),
                languageCode,
                layout,
                OccurrenceMerge.Add,
                async summary => new ImportedSource
                {
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    FileName = Path.GetFileName(filePath),
                    ContentHash = await _hasher.HashTextFileAsync(filePath, ct),
                    LanguageCode = languageCode,
                    ImportedAtUtc = DateTime.UtcNow,
                    WordsRead = summary.WordsRead,
                    NewWords = summary.NewWords,
                    UpdatedWords = summary.UpdatedWords,
                    IgnoredWords = summary.IgnoredWords
                },
                progress,
                ct),
            cancellationToken);

    public Task<Result<WordImportSummary>> ImportPackAsync(
        string packFilePath,
        WordPackInfo pack,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => RunAsync(
            packFilePath,
            ct => ImportCoreAsync(
                fileProgress => _packReader.ReadAsync(packFilePath, fileProgress, ct),
                pack.LanguageCode,
                layout,
                OccurrenceMerge.KeepHighest,
                summary => Task.FromResult(new ImportedSource
                {
                    Title = $"{pack.Title} ({pack.Version})",
                    FileName = pack.FileName,
                    // the checksum of the pack: the same pack is recognized in the history
                    ContentHash = pack.Sha256,
                    LanguageCode = pack.LanguageCode,
                    ImportedAtUtc = DateTime.UtcNow,
                    WordsRead = summary.WordsRead,
                    NewWords = summary.NewWords,
                    UpdatedWords = summary.UpdatedWords,
                    IgnoredWords = summary.IgnoredWords
                }),
                progress,
                ct),
            cancellationToken);

    private async Task<Result<WordImportSummary>> RunAsync(
        string filePath,
        Func<CancellationToken, Task<Result<WordImportSummary>>> import,
        CancellationToken cancellationToken)
    {
        try
        {
            return await import(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Word import cancelled for {FilePath}: nothing written", filePath);
            return Result<WordImportSummary>
                .Fail("Import annulé : rien n'a été ajouté à la base.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Word import failed for {FilePath}", filePath);
            return Result<WordImportSummary>
                .Fail($"L'import a échoué, rien n'a été ajouté à la base : {ex.Message}");
        }
    }

    private static async IAsyncEnumerable<(string Text, int Occurrences)> CountOnce(IAsyncEnumerable<string> words)
    {
        await foreach (string word in words)
            yield return (word, 1);
    }

    /// <summary>
    /// Reads the words (with their occurrences), analyzes them by batches for the keyboard, then writes
    /// the new and updated words and the history entry at once.
    /// </summary>
    /// <param name="read">The words, from a progress receiving the share of the file read</param>
    /// <param name="describeSource">The history entry, from the summary</param>
    private async Task<Result<WordImportSummary>> ImportCoreAsync(
        Func<IProgress<double>, IAsyncEnumerable<(string Text, int Occurrences)>> read,
        string languageCode,
        IKeyboardKeysLocator layout,
        OccurrenceMerge merge,
        Func<WordImportSummary, Task<ImportedSource>> describeSource,
        IProgress<WordImportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImportState state = new();

        // existing words, with their analyses: needed to know if the layout is already analyzed
        WordSearchCriteria searchCriteria =
            new WordQueryBuilder()
            .WithLanguages(languageCode)
            .WithAnalyses()
            .WithExclusion(WordExclusionFilter.All) // excluded words stay excluded
            .Build();

        foreach (Word existing in await _repository.SearchAsync(searchCriteria, cancellationToken))
            state.ExistingWords[existing.Text] = existing;

        Dictionary<string, int> batch = new(StringComparer.OrdinalIgnoreCase);

        progress?.Report(new WordImportProgress(WordImportPhase.Reading, 0, 0));

        // the reader reports the share of the file read: forward it with the word count
        InlineProgress<double> fileProgress =
            new(fraction => progress?.Report(new WordImportProgress(WordImportPhase.Reading, state.WordsRead, fraction)));

        await foreach ((string word, int occurrences) in read(fileProgress))
        {
            state.WordsRead++;

            if (state.NoMapWords.Contains(word))
                continue;

            if (!batch.TryAdd(word, occurrences))
                batch[word] += occurrences;

            if (batch.Count >= BatchSize)
            {
                Result<bool> batchResult = ProcessBatch(batch, languageCode, layout, merge, state);
                batch.Clear();
                if (!batchResult.Success)
                    return Result<WordImportSummary>.Fail(batchResult.Error);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        if (batch.Count > 0)
        {
            Result<bool> batchResult = ProcessBatch(batch, languageCode, layout, merge, state);
            if (!batchResult.Success)
                return Result<WordImportSummary>.Fail(batchResult.Error);
        }

        progress?.Report(new WordImportProgress(WordImportPhase.Reading, state.WordsRead, 1));

        WordImportSummary summary = new(
            WordsRead: state.WordsRead,
            NewWords: state.PendingNew.Count,
            UpdatedWords: state.PendingUpdated.Count,
            IgnoredWords: state.NoMapWords.Count,
            IgnoredSample: [.. state.NoMapWords.Order().Take(IgnoredSampleSize)]);

        ImportedSource source = await describeSource(summary);

        InlineProgress<double> saveProgress =
            new(fraction => progress?.Report(new WordImportProgress(WordImportPhase.Saving, state.WordsRead, fraction)));

        // the only write: words and history together, in one transaction
        await _repository.PersistImportAsync(
            newWords: [.. state.PendingNew.Values],
            updatedWords: [.. state.PendingUpdated.Values],
            source,
            saveProgress,
            cancellationToken);

        _logger.LogInformation(
            "Word import done for {Source}: {WordsRead} read, {NewWords} new, {UpdatedWords} updated, {IgnoredWords} ignored",
            source.FileName, summary.WordsRead, summary.NewWords, summary.UpdatedWords, summary.IgnoredWords);

        return Result<WordImportSummary>.Ok(summary);
    }

    /// <summary>
    /// Analyzes a batch and keeps the changes in memory.
    /// </summary>
    private Result<bool> ProcessBatch(
        Dictionary<string, int> batch,
        string languageCode,
        IKeyboardKeysLocator layout,
        OccurrenceMerge merge,
        ImportState state)
    {
        Result<WordProcessResult> resultProcess =
            _wordBatchProcessorOrchestrator.Process(batch, state.ExistingWords, languageCode, layout, merge);

        if (!resultProcess.Success)
        {
            return Result<bool>
                .Fail(resultProcess.Error);
        }

        WordProcessResult result = resultProcess.Value!;

        // the new words are known from now on: the next batches update them in memory
        foreach (Word w in result.NewWords)
        {
            state.ExistingWords[w.Text] = w;
            state.PendingNew[w.Text] = w;
        }

        // a word created by this import stays a new word, even if a later batch updates it
        foreach (Word w in result.UpdatedWords)
        {
            if (!state.PendingNew.ContainsKey(w.Text))
                state.PendingUpdated[w.Text] = w;
        }

        foreach (string txt in result.NoMapWords)
        {
            state.NoMapWords.Add(txt);
            _logger.LogDebug(
                "Analysis failed for text {FailedTxt} on layout {Layout}",
                txt,
                layout.GetKeyboardType);
        }

        return Result<bool>
            .Ok(true);
    }

    /// <summary>
    /// Calls back synchronously (unlike <see cref="Progress{T}"/>, no thread switch).
    /// </summary>
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class ImportState
    {
        public Dictionary<string, Word> ExistingWords { get; } = [];
        public HashSet<string> NoMapWords { get; } = [];
        public Dictionary<string, Word> PendingNew { get; } = [];
        public Dictionary<string, Word> PendingUpdated { get; } = [];
        public int WordsRead { get; set; }
    }
}
