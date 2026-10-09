using XyloType.Application.DTOs;

namespace XyloType.Application.Models;

/// <summary>
/// The language of the words typed on a keyboard, by default.
/// </summary>
public static class KeyboardLanguage
{
    public static string For(KeyboardLayoutEnumDto keyboard) => keyboard switch
    {
        KeyboardLayoutEnumDto.QwertyUs => "en",
        KeyboardLayoutEnumDto.QwertzDe => "de",
        _ => "fr",
    };
}
