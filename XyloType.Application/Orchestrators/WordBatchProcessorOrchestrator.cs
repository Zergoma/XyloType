using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Mappers;
using XyloType.Application.Models;
using XyloType.Application.ValueObjects;
using XyloType.Domain.Entities;
using XyloType.Domain.Models;

namespace XyloType.Application.Orchestrators;

public sealed class WordBatchProcessorOrchestrator : IWordBatchProcessorOrchestrator
{
    private readonly IKeyboardAnalyzerService _keyboardAnalyzerService;

    public WordBatchProcessorOrchestrator(
        IKeyboardAnalyzerService keyboardAnalyzerService)
    {
        _keyboardAnalyzerService = keyboardAnalyzerService;
    }

    public Result<WordProcessResult> Process(
        IReadOnlyDictionary<string, int> batch,
        IReadOnlyDictionary<string, Word> existingWords,
        string languageCode,
        IKeyboardKeysLocator keyBoardLocator,
        OccurrenceMerge merge = OccurrenceMerge.Add)
    {
        List <Word> newWordsList = [];
        List<Word> updatedWordsList = [];
        List<string> noMapWordsList = [];

        foreach ((string text, int count) in batch)
        {
            if (noMapWordsList.Contains(text))
                continue;

            if (existingWords.TryGetValue(text, out Word? word))
            {
                word.OccurrenceCount = merge == OccurrenceMerge.KeepHighest
                    ? Math.Max(word.OccurrenceCount, count)
                    : word.OccurrenceCount + count;

                if (!word.AnalyseExists(keyBoardLocator.GetKeyboardType))
                {
                    Result<UnitTextAnalysis> resu = EnsureAnalysis(text, keyBoardLocator.KeyLocator);
                    if (resu.Success)
                    {
                        WordAnalysis analysisEntityEnsured =
                            resu.Value!.ToEntity(keyBoardLocator.GetKeyboardType);
                        
                        word.AddAnalysis(analysisEntityEnsured);
                    }
                    else
                    {
                        // word exists but no map for this keyboardKeyLocator
                        // blacklist it
                        noMapWordsList.Add(text);
                    }
                }

                updatedWordsList.Add(word);
                continue;
            }

            Result<UnitTextAnalysis> analysis =
                _keyboardAnalyzerService.Analyze(text, keyBoardLocator.KeyLocator);

            if (!analysis.Success)
            {
                noMapWordsList.Add(text);
                continue;
            }

            Word newWord = new()
            {
                Text = text,
                LanguageCode = languageCode,
                Length = text.Length,
                OccurrenceCount = count
            };

            WordAnalysis analysisEntity =
                analysis.Value!.ToEntity(keyBoardLocator.GetKeyboardType);
            
            newWord.AddAnalysis(analysisEntity);
            newWordsList.Add(newWord);
        }

        return Result<WordProcessResult>
            .Ok(new WordProcessResult(
                NewWords: [.. newWordsList],
                UpdatedWords: [.. updatedWordsList],
                NoMapWords: [.. noMapWordsList])
            );
    }

    private Result<UnitTextAnalysis> EnsureAnalysis(
        string text,
        IReadOnlyDictionary<char, KeyInfo> map)
    {
        Result<UnitTextAnalysis> analysis =
            _keyboardAnalyzerService.Analyze(text, map);

        if (!analysis.Success)
            return Result<UnitTextAnalysis>
                .Fail($"Analysis failed for text : {text}");

        return Result<UnitTextAnalysis>
            .Ok(analysis.Value!);
    }
}