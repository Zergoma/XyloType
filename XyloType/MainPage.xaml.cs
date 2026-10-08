using Xylocopadream.UI.Maui;
using Xylocopadream.UI.Maui.Controls;

using XyloType.Application.Models.Themes;
using XyloType.Navigation;
using XyloType.ViewModels.Theme;

namespace XyloType;

/// <summary>
/// The single page of the app: the views are shown in its host by the <see cref="AppNavigator"/>.
/// </summary>
public partial class MainPage : ContentPage
{
    // order of the choices of the theme group
    private static readonly ThemeStateConfiguration[] s_themes =
        [ThemeStateConfiguration.Light, ThemeStateConfiguration.Dark, ThemeStateConfiguration.System];

    private readonly ThemeViewModel _theme;

    // top of the rail: the open exercise, only while it is shown
    private readonly NavRailItem _exerciseItem = new() { Icon = XdIcons.Keyboard, Key = AppSection.Exercise, IsVisible = false };

    // the word packs are offered once, at the first start with an empty database
    private const string WordPacksOfferedKey = "word_packs_offered";

    public MainPage(
        AppNavigator navigator,
        ThemeViewModel theme,
        AccentColorViewModel accent,
        ViewModels.Import.WordPacksViewModel wordPacks,
        Application.Interfaces.IUserDialogService dialogs)
    {
        InitializeComponent();

        // navigation rail: the sections of the app at the bottom
        Rail.TopItems.Add(_exerciseItem);
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Home, ToolTip = "Accueil", Key = AppSection.Home });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.List, ToolTip = "Éditeur d'exercices", Key = AppSection.Exercises });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Spellcheck, ToolTip = "Mots", Key = AppSection.Words });
        Rail.BottomItems.Add(new NavRailItem { Icon = XdIcons.Upload, ToolTip = "Import", Key = AppSection.Import });
        Rail.ItemClicked += async (_, item) => await navigator.GoToSectionAsync((AppSection)item.Key!);

        // theme: the group and the view model follow each other
        _theme = theme;
        ThemeGroup.SelectedIndex = Array.IndexOf(s_themes, theme.Theme);
        ThemeGroup.SelectedIndexChanged += (_, index) =>
        {
            if (index >= 0 && s_themes[index] != _theme.Theme)
                _theme.SetTheme(s_themes[index]);
        };
        theme.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeViewModel.Theme))
                ThemeGroup.SelectedIndex = Array.IndexOf(s_themes, _theme.Theme);
        };

        // main color
        AccentPicker.BindingContext = accent;
        AccentPicker.DefaultColor = Color.FromArgb(accent.DefaultAccent);
        AccentPicker.Presets = accent.Presets.Select(p => new ColorPreset(p.Name, Color.FromArgb(p.Hex))).ToList();

        navigator.CurrentViewChanged += view => ShowView(navigator, view);
        navigator.StateChanged += () => UpdateRail(navigator);
        navigator.BusyChanged += ShowBusy;

        navigator.Start();

        Loaded += async (_, _) => await OfferWordPacksAsync(navigator, wordPacks, dialogs);
    }

    /// <summary>
    /// First start, no word yet: the exercises of real words would be empty, the packs make the app ready at once.
    /// Asked once; the packs stay in Import > Mots.
    /// </summary>
    private static async Task OfferWordPacksAsync(
        AppNavigator navigator,
        ViewModels.Import.WordPacksViewModel wordPacks,
        Application.Interfaces.IUserDialogService dialogs)
    {
        if (Preferences.Default.Get(WordPacksOfferedKey, false) || !await wordPacks.HasNoWordsAsync())
            return;

        Preferences.Default.Set(WordPacksOfferedKey, true);

        bool download = await dialogs.ConfirmAsync(
            "Bienvenue dans XyloType",
            "La base de mots est vide. Voulez-vous télécharger un pack de mots prêt à l'emploi " +
            "(des dizaines de milliers de mots tirés de livres du domaine public) ?\n\n" +
            "Vous pourrez aussi le faire plus tard dans Import > Mots.",
            "Voir les packs",
            "Plus tard");

        if (download)
            await navigator.GoToSectionAsync(AppSection.Import);
    }

    private void ShowView(AppNavigator navigator, View view)
    {
        if (!ViewHost.Children.Contains(view))
            ViewHost.Children.Add(view);

        foreach (View child in ViewHost.Children.OfType<View>().ToList())
        {
            if (child == view)
                child.IsVisible = true;
            else if (navigator.IsKept(child))
                child.IsVisible = false;
            else
                ViewHost.Children.Remove(child);
        }
    }

    /// <summary>
    /// Highlights the button of the section being shown; shows the exercise button while one is open,
    /// as a keyboard while typing, as a chart on its results.
    /// </summary>
    private void UpdateRail(AppNavigator navigator)
    {
        bool showsResults = navigator.ExerciseView is MVVM.Views.StatisticView;

        _exerciseItem.IsVisible = navigator.ExerciseView is not null;
        _exerciseItem.Icon = showsResults ? XdIcons.BarChart : XdIcons.Keyboard;
        _exerciseItem.ToolTip = showsResults ? "Résultats de l'exercice" : "Exercice en cours";
        Rail.SelectedKey = navigator.CurrentSection;
    }

    private void ShowBusy(bool busy)
    {
        BusyVeil.IsVisible = busy;
        BusySpinner.IsRunning = busy;
    }

    private void AccentButton_Clicked(object? sender, EventArgs e)
        => AccentPopover.IsOpen = !AccentPopover.IsOpen;
}
