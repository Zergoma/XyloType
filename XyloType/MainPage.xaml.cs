using XyloType.MVVM.Controls;
using XyloType.Navigation;

namespace XyloType;

/// <summary>
/// The single page of the app: the views are shown in its host by the <see cref="AppNavigator"/>.
/// </summary>
public partial class MainPage : ContentPage
{
    private const double TooltipGap = 6;

    public MainPage(AppNavigator navigator)
    {
        InitializeComponent();

        navigator.CurrentViewChanged += view => ShowView(navigator, view);
        navigator.StateChanged += () => Rail.Update(navigator.CurrentSection, navigator.ExerciseView is not null, navigator.ExerciseView is MVVM.Views.StatisticView);
        Rail.SectionClicked += async section => await navigator.GoToSectionAsync(section);
        Rail.TooltipRequested += ShowTooltip;

        navigator.Start();
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

    private void ShowTooltip(string? text, View button)
    {
        if (text is null)
        {
            RailTooltip.IsVisible = false;
            return;
        }

        // position of the button in the page
        double y = button.Y;
        for (Element? parent = button.Parent; parent is VisualElement element && parent != Rail; parent = parent.Parent)
            y += element.Y;

        RailTooltipText.Text = text;
        RailTooltip.TranslationX = NavRail.RailWidth + TooltipGap;
        RailTooltip.TranslationY = y + (button.Height - RailTooltip.HeightRequest) / 2;
        RailTooltip.IsVisible = true;
    }
}
