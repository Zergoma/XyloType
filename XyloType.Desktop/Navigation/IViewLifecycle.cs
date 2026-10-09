namespace XyloType.Desktop.Navigation;

/// <summary>
/// A view told when the main window starts or stops showing it.
/// </summary>
public interface IViewLifecycle
{
    void OnAppearing();

    void OnDisappearing();
}
