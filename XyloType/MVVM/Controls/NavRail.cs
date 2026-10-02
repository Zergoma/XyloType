using Microsoft.Maui.Controls.Shapes;

using XyloType.Navigation;

namespace XyloType.MVVM.Controls;

/// <summary>
/// Main navigation, on the left edge of the main page, like the tool window bar of the JetBrains IDEs:
/// the open exercise at the top (only while it is shown), the sections of the app at the bottom.
/// Created once, with the main page.
/// </summary>
public class NavRail : ContentView
{
    public const double RailWidth = 60;

    // the sections of the app: icon and name (shown in the tooltip)
    private static readonly (AppSection Section, Geometry Icon, string Name)[] s_sections =
    [
        (AppSection.Home, Icons.Home, "Accueil"),
        (AppSection.Exercises, Icons.List, "Éditeur d'exercices"),
        (AppSection.Words, Icons.Words, "Mots"),
        (AppSection.Import, Icons.Import, "Import"),
    ];

    private readonly List<(AppSection Section, NavRailButton Button)> _buttons = [];
    private readonly NavRailButton _exerciseButton;
    private string _exerciseName = string.Empty;

    public NavRail()
    {
        // top: the open exercise
        _exerciseButton = AddButton(AppSection.Exercise, Icons.Keyboard, () => _exerciseName);
        _exerciseButton.IsVisible = false;

        VerticalStackLayout top = new()
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(10, 14, 10, 0),
            Children = { _exerciseButton },
        };

        // bottom: the sections
        VerticalStackLayout bottom = new()
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.End,
            Padding = new Thickness(10, 0, 10, 14),
        };

        foreach (var (section, icon, name) in s_sections)
            bottom.Children.Add(AddButton(section, icon, () => name));

        Grid rail = new() { WidthRequest = RailWidth, Children = { top, bottom } };
        rail.SetAppThemeColor(BackgroundColorProperty, Resource("BorderBgStdLight"), Resource("BorderBgStdDark"));
        Content = rail;
    }

    private NavRailButton AddButton(AppSection section, Geometry icon, Func<string> name)
    {
        NavRailButton button = new() { Icon = icon };
        button.Clicked += (_, _) => SectionClicked?.Invoke(section);
        button.PointerEntered += (_, _) => TooltipRequested?.Invoke(name(), button);
        button.PointerExited += (_, _) => TooltipRequested?.Invoke(null, button);

        _buttons.Add((section, button));
        return button;
    }

    public event Action<AppSection>? SectionClicked;

    /// <summary>
    /// The pointer entered (name) or left (null) a button: its tooltip is shown by the main page.
    /// </summary>
    public event Action<string?, View>? TooltipRequested;

    /// <summary>
    /// Highlights the button of the section being shown; shows the exercise button while one is open,
    /// as a keyboard while typing, as a chart on its results.
    /// </summary>
    public void Update(AppSection active, bool hasExercise, bool showsResults)
    {
        _exerciseButton.IsVisible = hasExercise;
        _exerciseButton.Icon = showsResults ? Icons.Chart : Icons.Keyboard;
        _exerciseName = showsResults ? "Résultats de l'exercice" : "Exercice en cours";

        foreach (var (section, button) in _buttons)
            button.IsActive = section == active;
    }

    private static Color Resource(string key)
        => Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color color
            ? color
            : Colors.Gray;
}
