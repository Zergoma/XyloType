using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Interfaces;
using XyloType.Application.Orchestrators;

namespace XyloType.Application.DI;

static internal class ApplicationOrchestratorModule
{
    public static IServiceCollection AddXyloTypeApplicationOrchestrators(this IServiceCollection services)
    {
        // ****************************************************************************************************
        // Order manner
        // ****************************************************************************************************
        services.AddTransient<IWordBatchProcessorOrchestrator, WordBatchProcessorOrchestrator>();   // depends ->  IKeyboardAnalyzerService
        services.AddTransient<IWordImportOrchestrator, WordImportOrchestrator>();                   // depends ->  IWordBatchProcessorOrchestrator
        services.AddTransient<IStarterPacksOrchestrator, StarterPacksOrchestrator>();               // depends ->  IWordImportOrchestrator, IExercisePackImporter
        // ****************************************************************************************************

        services.AddTransient<ICreateStringProviderOrchestrator, CreateStringProviderOrchestrator>();

        return services;
    }
}
