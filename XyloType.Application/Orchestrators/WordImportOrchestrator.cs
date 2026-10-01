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
    private readonly ILogger<WordImportOrchestrator> _logger;

    private const int BatchSize = 2000;

    public WordImportOrchestrator(
        IDactyloRepository dactyloRepository,
        IWordBatchProcessorOrchestrator processor,
        IWordStreamReader wordStreamingService,
        ILogger<WordImportOrchestrator> logger)
    {
        _repository = dactyloRepository;
        _wordBatchProcessorOrchestrator = processor;
        _wordStreamingService = wordStreamingService;
        _logger = logger;
    }

    public async Task<Result<WordImportSummary>> ImportAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null)
    {
        try
        {
            return await ImportCoreAsync(filePath, languageCode, layout, progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Word import failed for {FilePath}", filePath);
            return Result<WordImportSummary>
                .Fail($"L'import a échoué : {ex.Message}");
        }
    }

    private async Task<Result<WordImportSummary>> ImportCoreAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress)
    {
        ImportState state = new();

        // existing words, with their analyses: needed to know if the layout is already analyzed
        WordSearchCriteria searchCriteria =
            new WordQueryBuilder()
            .WithLanguages(languageCode)
            .WithAnalyses()
            .Build();

        foreach (Word existing in await _repository.SearchAsync(searchCriteria))
            state.ExistingWords[existing.Text] = existing;

        Dictionary<string, int> batch = new(StringComparer.OrdinalIgnoreCase);

        progress?.Report(new WordImportProgress(0, 0));

        // the reader reports the share of the file read: forward it with the word count
        InlineProgress<double> fileProgress =
            new(fraction => progress?.Report(new WordImportProgress(state.WordsRead, fraction)));

        await foreach (string word in _wordStreamingService.ReadWordsAsync(filePath, languageCode, fileProgress))
        {
            state.WordsRead++;

            if (state.NoMapWords.Contains(word))
                continue;

            if (!batch.TryAdd(word, 1))
                batch[word]++;

            if (batch.Count >= BatchSize)
            {
                Result<bool> flushResult = await FlushBatch(batch, languageCode, layout, state);
                batch.Clear();
                if (!flushResult.Success)
                    return Result<WordImportSummary>.Fail(flushResult.Error);
            }
        }

        if (batch.Count > 0)
        {
            Result<bool> flushResult = await FlushBatch(batch, languageCode, layout, state);
            if (!flushResult.Success)
                return Result<WordImportSummary>.Fail(flushResult.Error);
        }

        progress?.Report(new WordImportProgress(state.WordsRead, 1));

        WordImportSummary summary = new(
            WordsRead: state.WordsRead,
            NewWords: state.NewWordTexts.Count,
            UpdatedWords: state.UpdatedWordTexts.Count,
            IgnoredWords: state.NoMapWords.Count);

        _logger.LogInformation(
            "Word import done for {FilePath}: {WordsRead} read, {NewWords} new, {UpdatedWords} updated, {IgnoredWords} ignored",
            filePath, summary.WordsRead, summary.NewWords, summary.UpdatedWords, summary.IgnoredWords);

        return Result<WordImportSummary>.Ok(summary);
    }

    private async Task<Result<bool>> FlushBatch(
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

        // enforce via parameter name because same type
        await _repository.PersistWordsAsync(
            newWords: result.NewWords,
            updatedWords: result.UpdatedWords);

        // the new words are known from now on (with their database id)
        foreach (Word w in result.NewWords)
        {
            state.ExistingWords[w.Text] = w;
            state.NewWordTexts.Add(w.Text);
        }

        foreach (Word w in result.UpdatedWords)
        {
            if (!state.NewWordTexts.Contains(w.Text))
                state.UpdatedWordTexts.Add(w.Text);
        }

        // Keep in-memory txt that failed
        foreach (string txt in result.NoMapWords)
        {
            state.NoMapWords.Add(txt);
            _logger.LogDebug(
                "Analysis failed for text {FailedTxt} on layout {Layout}",
                txt,
                layout.GetKeyboardType);
        }

        _logger.LogInformation(
            "Batch persisted: {NewWordsCount} new, {UpdatedWordsCount} updated word(s)",
            result.NewWords.Length,
            result.UpdatedWords.Length);

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
        public HashSet<string> NewWordTexts { get; } = [];
        public HashSet<string> UpdatedWordTexts { get; } = [];
        public int WordsRead { get; set; }
    }
}
