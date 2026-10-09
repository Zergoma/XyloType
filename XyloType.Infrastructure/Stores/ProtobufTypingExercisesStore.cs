using Google.Protobuf;

using XyloType.Infrastructure.Protos;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Infrastructure.Mappers;

namespace XyloType.Infrastructure.Stores;

public class ProtobufTypingExercisesStore : IExerciseSettingsStore
{
    public async Task<Result<TypingExercices>> LoadAsync(string path)
    {
        if (!File.Exists(path))
            return Result<TypingExercices>
                .Fail($"File doesn't exist: {path}");

        ProtoTypingExerciceList exercicesList = new();
        using (var input = File.OpenRead(path))
        {
            exercicesList = ProtoTypingExerciceList.Parser.ParseFrom(input);
        }


        var resu = exercicesList.KeyboardLayout.Layout.MapToDtoEnum();
        if (!resu.Success)
            return Result<TypingExercices>.Fail(resu.Error);

        KeyBoardLayoutDto keyboarddto =
            new (
                resu.GetValue,
                exercicesList.KeyboardLayout.Name);

        TypingExercices toReturnList = new()
        {
            KeyboardLayout = keyboarddto,
        };

        foreach (ProtoExerciseSection protoSection in exercicesList.Sections)
            toReturnList.Sections.Add(ProtoTypingExercicePbToModelMapper.ToModel(protoSection));

        foreach (ProtoTypingExercice protoExerciceItem in exercicesList.Exercices)
        {
            Result<TypingExercise> typingExerciceResult
                = ProtoTypingExercicePbToModelMapper.ToModel(protoExerciceItem);

            if(!typingExerciceResult.Success)
            {
                return Result<TypingExercices>
                    .Fail(typingExerciceResult.Error);
            }

            toReturnList.Exercices.Add(typingExerciceResult.GetValue);
        }
            
        return Result<TypingExercices>.Ok(toReturnList);
    }

    public async Task<Result<bool>> SaveAsync(TypingExercices settings, string path)
    {
        Result<ProtoTypingExerciceList> resu
            = ProtoTypingExerciceModeltoPbMapper.ToProtobuf(settings);

        if (!resu.Success)
            return Result<bool>
                .Fail(resu.Error);

        ProtoTypingExerciceList exercicesList = resu.GetValue;

        using (var output = File.Create(path))
        {
            exercicesList.WriteTo(output);
        }

        return Result<bool>.Ok(true);
    }
}
