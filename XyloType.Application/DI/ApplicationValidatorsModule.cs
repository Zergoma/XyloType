using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.UseCases;
using XyloType.Application.Validators;

namespace XyloType.Application.DI;

internal static class ApplicationValidatorsModule
{
    public static IServiceCollection AddXyloTypeApplicationValidators(this IServiceCollection services)
    {
        services.AddTransient<IValidator<TypingExerciseCreateParameters>, TypingExerciseCreateParametersValidator>();
        services.AddTransient<IValidator<TypingExercise>, TypingExerciceValidator>();
        return services;
    }
}