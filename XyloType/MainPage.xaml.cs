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

    // the packs (exercises and words) are offered once, at the first start where something is missing
    private const string StarterPacksOfferedKey = "starter_packs_offered";

    public MainPage(
        AppNavigator navigator,
        ThemeViewModel theme,
        AccentColorViewModel accent,
        ViewModels.Import.StarterPacksViewModel starterPacks,
        ViewModels.Users.UsersViewModel users,
        Application.Interfaces.ICurrentUserService currentUser)
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

        // users: the avatar and the quick switch of the right rail
        _navigator = navigator;
        _users = users;
        _currentUser = currentUser;
        _starterPacks = starterPacks;
        starterPacks.PropertyChanged += (_, e) =>
        {
            // the veil tells what is being installed
            if (e.PropertyName == nameof(ViewModels.Import.StarterPacksViewModel.ProgressText))
                BusyLabel.Text = string.IsNullOrEmpty(starterPacks.ProgressText) ? LoadingText : starterPacks.ProgressText;
        };
        UserAvatar.BindingContext = users;
        UserMenu.BindingContext = users;
        currentUser.Changed += (_, _) => OnUsersChanged();

        navigator.Start();

        Loaded += async (_, _) =>
        {
            // without user, the change shows the users section (see OnUsersChanged)
            await currentUser.InitializeAsync();
            if (currentUser.HasUser)
                await OfferStarterPacksAsync();
        };
    }

    private readonly AppNavigator _navigator;
    private readonly ViewModels.Users.UsersViewModel _users;
    private readonly Application.Interfaces.ICurrentUserService _currentUser;
    private readonly ViewModels.Import.StarterPacksViewModel _starterPacks;

    private const string LoadingText = "Chargement…";

    // the first start waits for a first user: the sections open once it exists
    private bool _waitsForFirstUser;

    /// <summary>
    /// Without user, only the users section is open: the results need someone to belong to.
    /// The first user created opens the app, home first.
    /// </summary>
    private async void OnUsersChanged()
    {
        bool hasUser = _currentUser.HasUser;

        Rail.IsEnabled = hasUser;
        UserAvatar.IsVisible = hasUser;

        if (!hasUser)
        {
            _waitsForFirstUser = true;
            UserPopover.IsOpen = false;
            await _navigator.GoToSectionAsync(AppSection.Users);
            return;
        }

        if (_waitsForFirstUser)
        {
            _waitsForFirstUser = false;
            await _navigator.GoToSectionAsync(AppSection.Home);
            await OfferStarterPacksAsync();
        }
    }

    private void UserAvatar_Tapped(object? sender, TappedEventArgs e)
        => UserPopover.IsOpen = !UserPopover.IsOpen;

    private void UserMenuItem_Tapped(object? sender, TappedEventArgs e)
    {
        UserPopover.IsOpen = false;
        if (e.Parameter is ViewModels.Users.UserItemViewModel user)
            _users.SwitchToCommand.Execute(user);
    }

    private async void ManageUsers_Tapped(object? sender, TappedEventArgs e)
    {
        UserPopover.IsOpen = false;
        await _navigator.GoToSectionAsync(AppSection.Users);
    }

    /// <summary>
    /// First start: the exercise packs and a word pack make the app ready at once; installed in one go.
    /// Asked once (again at a later start when there was no connection); the packs stay in Exercices > Packs and Import > Mots.
    /// </summary>
    private async Task OfferStarterPacksAsync()
    {
        if (Preferences.Default.Get(StarterPacksOfferedKey, false))
            return;

        Application.Models.StarterPacksOffer? offer = await _starterPacks.GetOfferAsync();
        if (offer is null || !offer.CatalogsAvailable)
            return;

        Preferences.Default.Set(StarterPacksOfferedKey, true);
        if (offer.IsEmpty || !await _starterPacks.AskAsync(offer))
            return;

        Application.Models.StarterPacksSummary summary = await _navigator.RunBusyAsync(() => _starterPacks.InstallAsync(offer));

        // the home page shows the new exercises
        _navigator.RefreshCurrentView();
        await _starterPacks.ShowSummaryAsync(summary);
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
