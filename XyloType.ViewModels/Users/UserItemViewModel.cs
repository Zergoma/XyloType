using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Domain.Entities;

namespace XyloType.ViewModels.Users;

/// <summary>
/// A user, in the user list and in the quick switch of the rail.
/// </summary>
public partial class UserItemViewModel : ObservableObject
{
    public UserItemViewModel(UserProfile user, bool isCurrent, int attempts)
    {
        User = user;
        IsCurrent = isCurrent;
        Attempts = attempts;
    }

    public UserProfile User { get; }

    public int Id => User.Id;

    public string Name => User.Name;

    public string Initials => InitialsOf(User.Name);

    public bool IsCurrent { get; }

    public bool IsNotCurrent => !IsCurrent;

    public int Attempts { get; }

    public string AttemptsText => Attempts switch
    {
        0 => "Aucune partie jouée",
        1 => "1 partie jouée",
        _ => $"{Attempts:N0} parties jouées",
    };

    /// <summary>
    /// One or two letters for the avatar: the first letters of the first two words.
    /// </summary>
    public static string InitialsOf(string name)
    {
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length switch
        {
            0 => "?",
            1 => char.ToUpperInvariant(words[0][0]).ToString(),
            _ => string.Concat(char.ToUpperInvariant(words[0][0]), char.ToUpperInvariant(words[1][0])),
        };
    }
}
