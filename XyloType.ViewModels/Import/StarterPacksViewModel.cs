using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.ViewModels.Import;

/// <summary>
/// First start: offers the exercise packs and a word pack, and installs them in one go.
/// They also stay in Exercices > Packs and Import > Mots.
/// </summary>
public partial class StarterPacksViewModel : ObservableObject
{
    private readonly IStarterPacksOrchestrator _orchestrator;
    private readonly IUserDialogService _dialogs;
    private readonly IKeyBoardLayoutAvailableService _keyboards;
    private readonly IUserKeyboardLayoutPreferenceService _keyboardPreference;

    public StarterPacksViewModel(
        IStarterPacksOrchestrator orchestrator,
        IUserDialogService dialogs,
        IKeyBoardLayoutAvailableService keyboards,
        IUserKeyboardLayoutPreferenceService keyboardPreference)
    {
        _orchestrator = orchestrator;
        _dialogs = dialogs;
        _keyboards = keyboards;
        _keyboardPreference = keyboardPreference;
    }

    /// <summary>
    /// What is being installed.
    /// </summary>
    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    /// <summary>
    /// What is missing for the keyboard of the user (the one of the home page, else the first one).
    /// </summary>
    public async Task<StarterPacksOffer?> GetOfferAsync()
    {
        List<KeyBoardLayoutDto> keyboards = _keyboards.GetKeyBoardAvailable();
        Result<int> saved = _keyboardPreference.GetKeyboardType();
        KeyBoardLayoutDto? keyboard = keyboards.Find(k => saved.Success && (int)k.KeyBoardCode == saved.GetValue)
            ?? keyboards.FirstOrDefault();

        return keyboard is null ? null : await Task.Run(() => _orchestrator.GetOfferAsync(keyboard));
    }

    /// <returns>true if the user wants the packs</returns>
    public Task<bool> AskAsync(StarterPacksOffer offer)
    {
        List<string> lines = [];
        if (offer.ExercisePacks.Count > 0)
        {
            int count = offer.ExercisePacks.Sum(p => p.ExerciseCount);
            lines.Add($"• {count} exercices, des premières touches aux textes entiers ({string.Join(", ", offer.ExercisePacks.Select(p => p.Level))})");
        }

        if (offer.WordPack is WordPackInfo words)
            lines.Add($"• {words.WordCount:N0} mots pour les exercices de vrais mots ({words.Title})");

        return _dialogs.ConfirmAsync(
            "Prêt à taper ?",
            "XyloType peut télécharger tout de suite :\n\n" + string.Join("\n", lines) +
            "\n\nVous pourrez aussi le faire plus tard dans Exercices > Packs et Import > Mots.",
            "Télécharger",
            "Plus tard");
    }

    public async Task<StarterPacksSummary> InstallAsync(StarterPacksOffer offer)
    {
        // created on the UI thread: the reports come back on it
        Progress<string> progress = new(text => ProgressText = text);
        try
        {
            return await Task.Run(() => _orchestrator.InstallAsync(offer, progress));
        }
        finally
        {
            ProgressText = string.Empty;
        }
    }

    public Task ShowSummaryAsync(StarterPacksSummary summary)
    {
        List<string> done = [];
        if (summary.Exercises > 0)
            done.Add($"{summary.Exercises} exercices");
        if (summary.Words > 0)
            done.Add($"{summary.Words:N0} mots");

        string message = done.Count == 0
            ? "Rien n'a pu être installé."
            : "Installé : " + string.Join(" et ", done) + "." + (summary.Exercises > 0 ? " Les exercices sont sur l'accueil." : string.Empty);
        if (summary.Errors.Count > 0)
            message += "\n\nProblèmes :\n" + string.Join("\n", summary.Errors);

        return _dialogs.AlertAsync("Packs", message);
    }
}
