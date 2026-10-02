namespace XyloType.Navigation;

/// <summary>
/// A view told when the main page starts or stops showing it (what OnAppearing / OnDisappearing were for pages).
/// </summary>
public interface IViewLifecycle
{
    void OnAppearing();

    void OnDisappearing();
}
