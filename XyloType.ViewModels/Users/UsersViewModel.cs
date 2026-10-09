using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Entities;

namespace XyloType.ViewModels.Users;

/// <summary>
/// The users of the app: create, rename, delete, and choose who is typing.
/// The results of the exercises are kept per user.
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly ICurrentUserService _users;
    private readonly IExerciseAttemptRepository _attempts;
    private readonly IUserDialogService _dialogs;

    public UsersViewModel(
        ICurrentUserService users,
        IExerciseAttemptRepository attempts,
        IUserDialogService dialogs)
    {
        _users = users;
        _attempts = attempts;
        _dialogs = dialogs;

        _users.Changed += async (_, _) => await RefreshAsync();
    }

    public ObservableCollection<UserItemViewModel> Users { get; } = [];

    public bool HasUser => _users.HasUser;

    public bool HasNoUser => !HasUser;

    public string CurrentUserName => _users.CurrentUser?.Name ?? string.Empty;

    public string CurrentUserInitials => _users.CurrentUser is UserProfile user ? UserItemViewModel.InitialsOf(user.Name) : "?";

    public int NameMaxLength => UserProfile.NameMaxLength;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial string NewUserName { get; set; } = string.Empty;

    partial void OnNewUserNameChanged(string value) => Error = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string Error { get; set; } = string.Empty;

    public bool HasError => Error.Length > 0;

    /// <summary>
    /// Reads the users and their number of exercises done.
    /// </summary>
    public async Task RefreshAsync()
    {
        Dictionary<int, int> attempts = await _attempts.CountByUserAsync();

        Users.Clear();
        foreach (UserProfile user in _users.Users)
            Users.Add(new UserItemViewModel(user, user.Id == _users.CurrentUser?.Id, attempts.GetValueOrDefault(user.Id)));

        OnPropertyChanged(nameof(HasUser));
        OnPropertyChanged(nameof(HasNoUser));
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserInitials));
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(NewUserName);

    [RelayCommand(CanExecute = nameof(CanCreate))]
    public async Task Create()
    {
        Result<UserProfile> result = await _users.CreateAsync(NewUserName);
        if (!result.Success)
        {
            Error = result.Error;
            return;
        }

        NewUserName = string.Empty;
    }

    [RelayCommand]
    public void SwitchTo(UserItemViewModel user)
        => _users.SwitchTo(user.Id);

    [RelayCommand]
    public async Task Rename(UserItemViewModel user)
    {
        string? name = await _dialogs.PromptAsync(
            "Renommer",
            "Nouveau nom :",
            "Renommer",
            "Annuler",
            user.Name,
            UserProfile.NameMaxLength);

        if (name is null || name.Trim() == user.Name)
            return;

        Result<bool> result = await _users.RenameAsync(user.Id, name);
        if (!result.Success)
            await _dialogs.AlertAsync("Renommer", result.Error);
    }

    [RelayCommand]
    public async Task Delete(UserItemViewModel user)
    {
        string results = user.Attempts == 0 ? string.Empty : $" et ses résultats ({user.AttemptsText})";
        bool confirmed = await _dialogs.ConfirmAsync(
            "Supprimer l'utilisateur",
            $"Supprimer « {user.Name} »{results} ? C'est définitif.",
            "Supprimer",
            "Annuler");

        if (!confirmed)
            return;

        Result<bool> result = await _users.DeleteAsync(user.Id);
        if (!result.Success)
            await _dialogs.AlertAsync("Supprimer l'utilisateur", result.Error);
    }
}
