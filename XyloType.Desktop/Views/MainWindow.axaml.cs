using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

using Xylocopadream.UI.Avalonia.Controls;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Application.Models.Themes;
using XyloType.Desktop.Navigation;
using XyloType.ViewModels.Import;
using XyloType.ViewModels.Theme;
using XyloType.ViewModels.Users;

namespace XyloType.Desktop.Views;

/// <summary>
/// The main window: the views are shown in its host by the <see cref="AppNavigator"/>.
/// </summary>
public partial class MainWindow : Window
{
    // the packs (exercises and words) are offered once, at the first start where something is missing
    private const string StarterPacksOfferedKey = "starter_packs_offered";
    private const string LoadingText = "Chargement…";

    private readonly AppNavigator _navigator;
    private readonly ThemeViewModel _theme;
    private readonly UsersViewModel _users;
    private readonly ICurrentUserService _currentUser;
    private readonly StarterPacksViewModel _starterPacks;
    private readonly ISettingsStore _settings;

    // the first start waits for a first user: the sections open once it exists
    private bool _waitsForFirstUser;

    public MainWindow(
        AppNavigator navigator,
        ThemeViewModel theme,
        UsersViewModel users,
        ICurrentUserService currentUser,
        StarterPacksViewModel starterPacks,
        ISettingsStore settings)
    {
        InitializeComponent();

        _navigator = navigator;
        _theme = theme;
        _users = users;
        _currentUser = currentUser;
        _starterPacks = starterPacks;
        _settings = settings;

        // the theme switcher shows the theme of the user, and remembers the next one
        Themes.Variant = theme.Theme switch
        {
            ThemeStateConfiguration.Light => ThemeVariant.Light,
            ThemeStateConfiguration.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        Themes.PropertyChanged += (_, e) =>
        {
            if (e.Property == ThemeSwitcher.VariantProperty)
                _theme.SetTheme(ToConfiguration(Themes.Variant));
        };

        UserButton.DataContext = users;
        currentUser.Changed += (_, _) => OnUsersChanged();

        starterPacks.PropertyChanged += (_, e) =>
        {
            // the veil tells what is being installed
            if (e.PropertyName == nameof(StarterPacksViewModel.ProgressText))
                BusyText.Text = string.IsNullOrEmpty(starterPacks.ProgressText) ? LoadingText : starterPacks.ProgressText;
        };

        navigator.CurrentViewChanged += view => ViewHost.Content = view;
        navigator.StateChanged += UpdateRail;
        navigator.BusyChanged += busy => BusyVeil.IsVisible = busy;

        navigator.Start();

        Opened += async (_, _) =>
        {
            // without user, the change shows the users section (see OnUsersChanged)
            await currentUser.InitializeAsync();
            if (currentUser.HasUser)
                await OfferStarterPacksAsync();
        };
    }

    private static ThemeStateConfiguration ToConfiguration(ThemeVariant variant)
        => variant == ThemeVariant.Light ? ThemeStateConfiguration.Light
            : variant == ThemeVariant.Dark ? ThemeStateConfiguration.Dark
            : ThemeStateConfiguration.System;

    #region Rail

    private async void Section_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: string tag } && Enum.TryParse(tag, out AppSection section))
            await _navigator.GoToSectionAsync(section);

        // the rail shows where the navigator is (a refused navigation unchecks the button)
        UpdateRail();
    }

    private void ExerciseButton_Click(object? sender, RoutedEventArgs e)
        => UpdateRail();

    /// <summary>
    /// Highlights the button of the section shown; shows the exercise button while one is open,
    /// as a keyboard while typing, as a chart on its results.
    /// </summary>
    private void UpdateRail()
    {
        foreach (ToggleButton button in SectionButtons.Children.OfType<ToggleButton>())
            button.IsChecked = button.Tag is string tag && tag == _navigator.CurrentSection.ToString();

        bool showsResults = _navigator.ExerciseView is StatisticView;
        ExerciseButton.IsVisible = _navigator.ExerciseView is not null;
        ExerciseButton.IsChecked = true;
        ExerciseIcon.Data = (Avalonia.Media.Geometry?)this.FindResource(showsResults ? "App.Icon.Chart" : "App.Icon.Keyboard");
        ToolTip.SetTip(ExerciseButton, showsResults ? "Résultats de l'exercice" : "Exercice en cours");
    }

    #endregion

    #region Users

    /// <summary>
    /// Without user, only the users section is open: the results need someone to belong to.
    /// The first user created opens the app, home first.
    /// </summary>
    private async void OnUsersChanged()
    {
        bool hasUser = _currentUser.HasUser;

        SectionButtons.IsEnabled = hasUser;
        UserButton.IsVisible = hasUser;

        if (!hasUser)
        {
            _waitsForFirstUser = true;
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

    /// <summary>
    /// Opens the quick switch; in the users section, closes the section instead.
    /// </summary>
    private async void UserButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_navigator.CurrentSection != AppSection.Users)
            return;

        UserButton.Flyout?.Hide();
        await _navigator.CloseUsersAsync();
    }

    private void UserItem_Click(object? sender, RoutedEventArgs e)
    {
        UserButton.Flyout?.Hide();
        if (sender is Control { DataContext: UserItemViewModel user })
            _users.SwitchToCommand.Execute(user);
    }

    private async void ManageUsers_Click(object? sender, RoutedEventArgs e)
    {
        UserButton.Flyout?.Hide();
        await _navigator.GoToSectionAsync(AppSection.Users);
    }

    /// <summary>
    /// Escape closes the users section (once there is a user: at the first start, there is nowhere to go back).
    /// </summary>
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || e.Key != Key.Escape || _navigator.CurrentSection != AppSection.Users || !_currentUser.HasUser)
            return;

        e.Handled = true;
        await _navigator.CloseUsersAsync();
    }

    #endregion

    #region Starter packs

    /// <summary>
    /// First start: the exercise packs and a word pack make the app ready at once; installed in one go.
    /// Asked once (again at a later start when there was no connection); the packs stay in Exercices > Packs and Import > Mots.
    /// </summary>
    private async Task OfferStarterPacksAsync()
    {
        if (_settings.Get(StarterPacksOfferedKey, false))
            return;

        StarterPacksOffer? offer = await _starterPacks.GetOfferAsync();
        if (offer is null || !offer.CatalogsAvailable)
            return;

        _settings.Set(StarterPacksOfferedKey, true);
        if (offer.IsEmpty || !await _starterPacks.AskAsync(offer))
            return;

        StarterPacksSummary summary = await _navigator.RunBusyAsync(() => _starterPacks.InstallAsync(offer));

        // the home page shows the new exercises
        _navigator.RefreshCurrentView();
        await _starterPacks.ShowSummaryAsync(summary);
    }

    #endregion
}
