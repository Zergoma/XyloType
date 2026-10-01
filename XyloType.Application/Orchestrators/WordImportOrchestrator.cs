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
        IContentHasher hasher,
        ILogger<WordImportOrchestrator> logger)
    {
        _repository = dactyloRepository;
        _wordBatchProcessorOrchestrator = processor;
        _wordStreamingService = wordStreamingService;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<Result<WordImportSummary>> ImportAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ImportCoreAsync(filePath, languageCode, layout, progress, cancellationToken);
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

    private async Task<Result<WordImportSummary>> ImportCoreAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
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

        await foreach (string word in _wordStreamingService.ReadWordsAsync(filePath, languageCode, fileProgress, cancellationToken))
        {
            state.WordsRead++;

            if (state.NoMapWords.Contains(word))
                continue;

            if (!batch.TryAdd(word, 1))
                batch[word]++;

            if (batch.Count >= BatchSize)
            {
                Result<bool> batchResult = ProcessBatch(batch, languageCode, layout, state);
                batch.Clear();
                if (!batchResult.Success)
                    return Result<WordImportSummary>.Fail(batchResult.Error);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        if (batch.Count > 0)
        {
            Result<bool> batchResult = ProcessBatch(batch, languageCode, layout, state);
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

        ImportedSource source = new()
        {
            Title = Path.GetFileNameWithoutExtension(filePath),
            FileName = Path.GetFileName(filePath),
            ContentHash = await _hasher.HashTextFileAsync(filePath, cancellationToken),
            LanguageCode = languageCode,
            ImportedAtUtc = DateTime.UtcNow,
            WordsRead = summary.WordsRead,
            NewWords = summary.NewWords,
            UpdatedWords = summary.UpdatedWords,
            IgnoredWords = summary.IgnoredWords
        };

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
            "Word import done for {FilePath}: {WordsRead} read, {NewWords} new, {UpdatedWords} updated, {IgnoredWords} ignored",
            filePath, summary.WordsRead, summary.NewWords, summary.UpdatedWords, summary.IgnoredWords);

        return Result<WordImportSummary>.Ok(summary);
    }

    /// <summary>
    /// Analyzes a batch and keeps the changes in memory.
    /// </summary>
    private Result<bool> ProcessBatch(
        Dictionary<string, int> batch,
        string languageCode,
        IKeyboardKeysLocator layout,
        ImportState state)
    {
        Result<WordProcessResult> resultProcess =
            _wordBatchProcessorOrchestrator.Process(batch, state.ExistingWords, languageCode, layout);

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
