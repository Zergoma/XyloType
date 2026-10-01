using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Mappers;
using XyloType.Domain.Enums;
using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.ValueObjects;

namespace XyloType.Application.Orchestrators;

public class CreateStringProviderOrchestrator : ICreateStringProviderOrchestrator
{
    private readonly IPseudoWordBatchGenerator _pseudoWordBatchGenerator;
    private readonly ITypingExerciseWordNumberService _typingExerciceWordNumberService;
    private readonly ITypingExerciseLineNumberService _typingExerciceLineNumberService;
    private readonly IEditorSplitCharProvider _editorSplitCharProvider;
    private readonly IImportedWordsGenerator _importedWordsGenerator;

    public CreateStringProviderOrchestrator(
        IPseudoWordBatchGenerator pseudoWordBatchGenerator,
        ITypingExerciseWordNumberService typingExerciceWordNumberService,
        ITypingExerciseLineNumberService typingExerciceLineNumberService,
        IEditorSplitCharProvider editorSplitCharProvider,
        IImportedWordsGenerator importedWordsGenerator)
    {
        _pseudoWordBatchGenerator = pseudoWordBatchGenerator;
        _typingExerciceWordNumberService = typingExerciceWordNumberService;
        _typingExerciceLineNumberService = typingExerciceLineNumberService;
        _editorSplitCharProvider = editorSplitCharProvider;
        _importedWordsGenerator = importedWordsGenerator;
    }

    public Result<IStringsProvider> Create(
        TypingExercise exercice,
        KeyBoardLayoutDto selectedKeyboard)
    {

        if (exercice.TextDataType is TypingTextDataStatic itemStatic)
        {
            return Result<IStringsProvider>
                .Ok(new TypingExerciceStaticDataProducer(_editorSplitCharProvider, itemStatic));
        }

        if (exercice.TextDataType is TypingTextDataDynamic itemDynamic)
        {
            if (itemDynamic.GeneratedTypeSource == GeneratedTypeSource.PseudoWords)
            {
                return Result<IStringsProvider>
                    .Ok(new TypingExerciceDynamicPseudoWordsProducer(
                        _pseudoWordBatchGenerator,
                        _typingExerciceWordNumberService,
                        _typingExerciceLineNumberService,
                        itemDynamic,
                        exercice.AllowedCharacters
                        ));
            }

            if (itemDynamic.GeneratedTypeSource == GeneratedTypeSource.Words)
            {
                Result<KeyboardLayout> layoutResult = selectedKeyboard.KeyBoardCode.ToDomainEnum();
                if (!layoutResult.Success)
                {
                    return Result<IStringsProvider>
                        .Fail(layoutResult.Error);
                }

                return Result<IStringsProvider>
                    .Ok(new TypingExerciseDynamicWordsProducer(
                        _importedWordsGenerator,
                        _typingExerciceWordNumberService,
                        _typingExerciceLineNumberService,
                        itemDynamic,
                        exercice.AllowedCharacters,
                        layoutResult.GetValue));
            }
        }
        return Result<IStringsProvider>
            .Fail("No static or dynamic configuration found");

    }

    public class TypingExerciceStaticDataProducer : IStringsProvider
    {
        private readonly char _splitChar;
        private readonly TypingTextDataStatic _staticItem;

        public TypingExerciceStaticDataProducer(
            IEditorSplitCharProvider editorSplitCharProvider,
            TypingTextDataStatic staticItem)
        {
            _splitChar = editorSplitCharProvider.GetSplitCharacter();
            _staticItem = staticItem;
        }

        public async Task<Result<IEnumerable<string>>> GetStringsAsync()
        {
            string generatedRawStored = _staticItem.GeneratedText;

            string[] rawList = generatedRawStored.Split(_splitChar);
            List<string> filteredLines = [];
            foreach (var line in rawList)
            {
                // For stability, we filtred the control char
                string cleaned = new([.. line.Where(c => !char.IsControl(c))]);
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    continue;
                }
                filteredLines.Add(cleaned);
            }

            return Result<IEnumerable<string>>
                .Ok(filteredLines);
        }
    }

    public class TypingExerciceDynamicPseudoWordsProducer : IStringsProvider
    {
        private readonly IPseudoWordBatchGenerator _pseudoWordBatchGenerator;
        private readonly ITypingExerciseWordNumberService _typingExerciceWordNumberService;
        private readonly ITypingExerciseLineNumberService _typingExerciceLineNumberService;
        private readonly TypingTextDataDynamic _itemDynamic;
        private readonly string _allowedLetters;

        public TypingExerciceDynamicPseudoWordsProducer(
            IPseudoWordBatchGenerator pseudoWordBatchGenerator,
            ITypingExerciseWordNumberService typingExerciceWordNumberService,
            ITypingExerciseLineNumberService typingExerciceLineNumberService,
            TypingTextDataDynamic itemDynamic,
            string allowedLetters)
        {
            _pseudoWordBatchGenerator = pseudoWordBatchGenerator;
            _typingExerciceWordNumberService = typingExerciceWordNumberService;
            _typingExerciceLineNumberService = typingExerciceLineNumberService;
            _itemDynamic = itemDynamic;
            _allowedLetters = allowedLetters;
        }

        public async Task<Result<IEnumerable<string>>> GetStringsAsync()
        {
            List<string> all = [];

            for (int i = 0; i < _typingExerciceLineNumberService.LineNumber; i++)
            {
                Result<List<string>> generatedPseudoWordResult =
                    _pseudoWordBatchGenerator.Generate(
                        _typingExerciceWordNumberService.ItemNumber,
                        new PseudoWordOptions()
                        {
                            AllowedChars = _allowedLetters,
                            MinLength = _itemDynamic.LengthMin,
                            MaxLength = _itemDynamic.LengthMax,
                        });

                if (!generatedPseudoWordResult.Success)
                {
                    return Result<IEnumerable<string>>
                        .Fail(generatedPseudoWordResult.Error);
                }

                all.Add(string.Join(' ', generatedPseudoWordResult.GetValue));
            }

            return Result<IEnumerable<string>>
                .Ok(all);
        }
    }
}
