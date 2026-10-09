using XyloType.Infrastructure.Protos;

using XyloType.Application;
using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;

using static XyloType.Infrastructure.Protos.ProtoKeyboardLayout.Types;
using static XyloType.Infrastructure.Protos.ProtoTypingTextDataDynamic.Types;

namespace XyloType.Infrastructure.Mappers;

public static class ProtoTypingExercicePbToModelMapper
{
    private static Result<TypingTextData> PbToModelCreateDynamic(ProtoTypingExercice proto)
    {
        if (proto.TextDataTypeCase != ProtoTypingExercice.TextDataTypeOneofCase.DynamicTextData)
        {
            return Result<TypingTextData>.Fail("Not a dynamic");
        }

        Result<GeneratedTypeSource> generationTypeSourceResult
            = proto.DynamicTextData.GeneratedTypeSource.MapToDtoEnum();


        if (!generationTypeSourceResult.Success)
        {
            return Result<TypingTextData>
                .Fail(generationTypeSourceResult.Error);
        }


        var curr = new TypingTextDataDynamic()
        {
            LengthMin = (int)proto.DynamicTextData.LengthMin,
            LengthMax = (int)proto.DynamicTextData.LengthMax,
            GeneratedTypeSource = generationTypeSourceResult.GetValue,
        };

        foreach (var lang in proto.DynamicTextData.LanguagesSelected)
        {
            curr.LanguagesSelected.Add(lang);
        }
        return Result<TypingTextData>.Ok(curr);
    }

    public static ExerciseSection ToModel(ProtoExerciseSection proto)
        => new()
        {
            Id = new(proto.Id.ToByteArray()),
            Title = proto.Title,
            PackId = string.IsNullOrEmpty(proto.PackId) ? null : proto.PackId,
            PackVersion = string.IsNullOrEmpty(proto.PackVersion) ? null : proto.PackVersion,
        };

    public static Result<TypingExercise> ToModel(ProtoTypingExercice proto)
    {
        TypingExercise typingExercice
            = new()
            {
                Name = proto.Name,
                Description = proto.Description,
                AllowedCharacters = proto.AllowedCharacters,
                Id = new (proto.Id.ToByteArray()),
                // no section yet: the exercise goes to the default one when the list is normalized
                SectionId = proto.SectionId.Length == 16 ? new(proto.SectionId.ToByteArray()) : Guid.Empty,
            };

        Result<TypingTextData> staticDynamicResult = proto.TextDataTypeCase switch
        {
            ProtoTypingExercice.TextDataTypeOneofCase.StaticTextData =>
                Result<TypingTextData>.Ok(new TypingTextDataStatic()
                {
                    GeneratedText = proto.StaticTextData.GeneratedText
                }),

            ProtoTypingExercice.TextDataTypeOneofCase.DynamicTextData =>
                PbToModelCreateDynamic(proto),

            _ => Result<TypingTextData>.Fail("The field is empty, it must be static or dynamic type"),
        };

        if (!staticDynamicResult.Success)
            return Result<TypingExercise>
                .Fail(staticDynamicResult.Error);

        typingExercice.TextDataType = staticDynamicResult.GetValue;


        return Result<TypingExercise>
            .Ok(typingExercice);
    }
}

public static class ProtoTypingExerciceModeltoPbMapper
{
    public static Result<ProtoTypingExerciceList> ToProtobuf(TypingExercices settings)
    {
        ProtoTypingExerciceList exercicesList = new();

        Result<ProtoKeyboardLayoutType> MappedKeyboardTypeResult
                    = settings.KeyboardLayout.KeyBoardCode.MapToPbEnum();
        if (!MappedKeyboardTypeResult.Success)
        {
            return Result<ProtoTypingExerciceList>
                .Fail(MappedKeyboardTypeResult.Error);
        }

        exercicesList.KeyboardLayout = new()
        {
            Name = settings.KeyboardLayout.KeyBoardHumanFriendly,
            Layout = MappedKeyboardTypeResult.GetValue
        };



        foreach (ExerciseSection section in settings.Sections)
        {
            exercicesList.Sections.Add(new ProtoExerciseSection
            {
                Id = Google.Protobuf.ByteString.CopyFrom(section.Id.ToByteArray()),
                Title = section.Title,
                PackId = section.PackId ?? string.Empty,
                PackVersion = section.PackVersion ?? string.Empty,
            });
        }

        foreach (TypingExercise typingExercice in settings.Exercices)
        {
            ProtoTypingExercice protoTypingExo = new()
            {
                Name = typingExercice.Name,
                Description = typingExercice.Description,
                AllowedCharacters = typingExercice.AllowedCharacters,
                Id = Google.Protobuf.ByteString.CopyFrom(typingExercice.Id.ToByteArray()),
                SectionId = Google.Protobuf.ByteString.CopyFrom(typingExercice.SectionId.ToByteArray()),
            };


            Result<ProtoTypingExercice> MappedToDynamicStaticResu
                = typingExercice.TextDataType switch
                {
                    TypingTextDataStatic staticItem => ModelToPb_Static(protoTypingExo, staticItem),
                    TypingTextDataDynamic dynamicItem => ModelToPb_Dynamic(protoTypingExo, dynamicItem)
                };

            if (!MappedToDynamicStaticResu.Success)
                return Result<ProtoTypingExerciceList>.Fail(MappedToDynamicStaticResu.Error);


            protoTypingExo = MappedToDynamicStaticResu.GetValue;
            exercicesList.Exercices.Add(protoTypingExo);
        }

        return Result<ProtoTypingExerciceList>.Ok(exercicesList);
    }

    private static Result<ProtoTypingExercice> ModelToPb_Static(
        ProtoTypingExercice protoTypingExo,
        TypingTextDataStatic staticItem)
    {
        protoTypingExo.StaticTextData
            = new ProtoTypingTextDataStatic()
            {
                GeneratedText = staticItem.GeneratedText
            };

        return Result<ProtoTypingExercice>
            .Ok(protoTypingExo);
    }

    private static Result<ProtoTypingExercice> ModelToPb_Dynamic(
        ProtoTypingExercice protoTypingExo,
        TypingTextDataDynamic dynamicItem)
    {
        Result<ProtoGeneratedTypeSource> sourceTypeResult
            = dynamicItem.GeneratedTypeSource.MapToPbEnum();

        if (!sourceTypeResult.Success)
        {
            return Result<ProtoTypingExercice>
                .Fail(sourceTypeResult.Error);
        }


        protoTypingExo.DynamicTextData
            = new ProtoTypingTextDataDynamic()
            {
                LengthMin = (uint)dynamicItem.LengthMin,
                LengthMax = (uint)dynamicItem.LengthMax,
                GeneratedTypeSource = sourceTypeResult.GetValue
            };

        foreach (string lang in dynamicItem.LanguagesSelected)
        {
            protoTypingExo.DynamicTextData.LanguagesSelected.Add(lang);
        }

        return Result<ProtoTypingExercice>
            .Ok(protoTypingExo);
    }

}
