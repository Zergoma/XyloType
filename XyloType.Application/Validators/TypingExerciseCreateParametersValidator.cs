using FluentValidation;

using XyloType.Application.Models;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.UseCases;

namespace XyloType.Application.Validators;

public class TypingExerciseCreateParametersValidator : AbstractValidator<TypingExerciseCreateParameters>
{
    public TypingExerciseCreateParametersValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is empty");

        RuleFor(x => x.AllowedLetters)
            .NotEmpty()
            .WithMessage("No letters selected");

        RuleFor(x => x.KeyBoardLayoutDto)
            .NotNull()
            .WithMessage("You need to select a keybord");
    }
}



public class TypingTextDataStaticValidator : AbstractValidator<TypingTextDataStatic>
{
    public TypingTextDataStaticValidator()
    {
        RuleFor(x => x.GeneratedText)
            .NotEmpty()
            .MaximumLength(ExerciseSizeLimits.MaxTextLength)
            .WithMessage(x => $"Le texte fait {x.GeneratedText.Length:N0} caractères : raccourcissez-le à {ExerciseSizeLimits.MaxTextLength:N0} au plus.");
    }
}

public class TypingTextDataDynamicValidator : AbstractValidator<TypingTextDataDynamic>
{
    public TypingTextDataDynamicValidator()
    {
        RuleFor(x => x.LengthMin)
            .GreaterThan(0);

        RuleFor(x => x.LengthMax)
            .GreaterThanOrEqualTo(x => x.LengthMin);
    }
}

public class TypingExerciceValidator : AbstractValidator<TypingExercise>
{
    public TypingExerciceValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .WithMessage("ID is null")
            .NotEmpty()
            .WithMessage("No ID");

        RuleFor(x => x.Name)
            .NotNull()
            .WithMessage("Name is null")
            .NotEmpty()
            .WithMessage("Name is empty");

        RuleFor(x => x.AllowedCharacters)
            .NotNull()
            .WithMessage("AllowedCharacters is null")
            .NotEmpty()
            .WithMessage("AllowedCharacters is empty");


        RuleFor(x => x.TextDataType)
            .Custom((value, context) =>
            {
                switch (value)
                {
                    case TypingTextDataStatic data:
                        {
                            var result = new TypingTextDataStaticValidator().Validate(data);

                            foreach (var error in result.Errors)
                            {
                                context.AddFailure(error);
                            }

                            break;
                        }

                    case TypingTextDataDynamic data:
                        {
                            var result = new TypingTextDataDynamicValidator().Validate(data);

                            foreach (var error in result.Errors)
                            {
                                context.AddFailure(error);
                            }

                            break;
                        }
                }
            });


    }
}