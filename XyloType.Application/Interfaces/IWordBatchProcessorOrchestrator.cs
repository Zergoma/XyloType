using XyloType.Application;
using XyloType.Application.ValueObjects;
using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces
{
    public interface IWordBatchProcessorOrchestrator
    {
        Result<WordProcessResult> Process(
            IReadOnlyDictionary<string, int> batch,
            IReadOnlyDictionary<string, Word> existingWords,
            string languageCode,
            IKeyboardKeysLocator layout,
            Models.OccurrenceMerge merge = Models.OccurrenceMerge.Add);
    }
}