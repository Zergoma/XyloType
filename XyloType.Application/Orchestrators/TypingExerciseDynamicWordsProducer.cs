using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Domain.Enums;

namespace XyloType.Application.Orchestrators;

/// <summary>
/// Builds the lines of a dynamic exercise from the imported words
/// (exercise languages, keyboard, length range and allowed letters).
/// </summary>
public class TypingExerciseDynamicWordsProducer : IStringsProvider
{
    private readonly IImportedWordsGenerator _wordsGenerator;
    private readonly ITypingExerciseWordNumberService _wordNumberService;
    private readonly ITypingExerciseLineNumberService _lineNumberService;
    private readonly TypingTextDataDynamic _itemDynamic;
    private readonly string _allowedLetters;
    private readonly KeyboardLayout _layout;

    public TypingExerciseDynamicWordsProducer(
        IImportedWordsGenerator wordsGenerator,
        ITypingExerciseWordNumberService wordNumberService,
        ITypingExerciseLineNumberService lineNumberService,
        TypingTextDataDynamic itemDynamic,
        string allowedLetters,
        KeyboardLayout layout)
    {
        _wordsGenerator = wordsGenerator;
        _wordNumberService = wordNumberService;
        _lineNumberService = lineNumberService;
        _itemDynamic = itemDynamic;
        _allowedLetters = allowedLetters;
        _layout = layout;
    }

    public async Task<Result<IEnumerable<string>>> GetStringsAsync()
    {
        int wordsPerLine = Math.Max(1, _wordNumberService.ItemNumber);
        int lineCount = _lineNumberService.LineNumber;

        Result<List<string>> wordsResult =
            await _wordsGenerator.GenerateAsync(
                new ImportedWordsOptions(
                    _itemDynamic.LanguagesSelected,
                    _allowedLetters,
                    _itemDynamic.LengthMin,
                    _itemDynamic.LengthMax,
                    _layout),
                wordsPerLine * lineCount);

        if (!wordsResult.Success)
        {
            return Result<IEnumerable<string>>
                .Fail(wordsResult.Error);
        }

        string[] lines =
            [.. wordsResult.GetValue
                .Chunk(wordsPerLine)
                .Select(lineWords => string.Join(' ', lineWords))];

        return Result<IEnumerable<string>>.Ok(lines);
    }
}
