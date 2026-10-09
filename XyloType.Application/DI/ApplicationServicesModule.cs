using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Services;

namespace XyloType.Application.DI;

internal static class ApplicationServicesModule
{
    public static IServiceCollection AddXyloTypeApplicationServices(this IServiceCollection services)
    {
        // ********************************************************************************************
        // Zero dependancies services
        // ********************************************************************************************
        services.AddTransient<IInputCharMapperService, InputCharMapperService>();
        services.AddTransient<IKeyboardAnalyzerService, KeyboardAnalyzerService>();
        services.AddTransient<IKeyBoardLayoutAvailableService, KeyBoardLayoutAvailableService>();
        services.AddTransient<ILanguageAvailableService, LanguageAvailableService>();
        services.AddTransient<IPseudoWordGeneratorService, PseudoWordGeneratorService>();
        services.AddTransient<IStringsProvider, DevStringsProvider>();
        services.AddTransient<IGenerationTypeSourceAvailableService, GenerationTypeSourceAvailableService>();
        services.AddTransient<IEditorSplitCharProvider, EditorSplitCharProvider>();
        services.AddTransient<IGuidProvider, GuidProvider>();
        services.AddTransient<IStatColorScale, StatColorScale>();


        services.AddSingleton<ITypingExerciseWordNumberService, TypingExerciseWordNumberService>();
        services.AddSingleton<ITypingExerciseLineNumberService, TypingExerciseLineNumberService>();
        services.AddSingleton<ITypingExerciseRunService, TypingExerciseRunService>();
        services.AddTransient<IExercisesEditSession, ExercisesEditSession>();
        services.AddTransient<IImportedWordsGenerator, ImportedWordsGenerator>();
        services.AddTransient<IImportDuplicateChecker, ImportDuplicateChecker>();
        services.AddSingleton<IScoreCatalog, ScoreCatalog>();

        // users and their results
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddTransient<IExerciseProgressService, ExerciseProgressService>();

        // exercise packs
        services.AddTransient<IExercisePackImporter, ExercisePackImporter>();

        // TODO
        // need to add qwerty etc keyboard keys locators
        services.AddTransient<IKeyboardKeysLocator, AzertyKeysLocator>();
        // ********************************************************************************************

        services.AddTransient<IPseudoWordBatchGenerator, PseudoWordBatchGenerator>();   // depends -> IPseudoWordGeneratorService


        return services;
    }
}
