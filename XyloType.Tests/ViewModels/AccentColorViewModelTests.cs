using FluentAssertions;

using NSubstitute;

using XyloType.Application.Interfaces;
using XyloType.ViewModels.Theme;

namespace XyloType.Tests.ViewModels;

public class AccentColorViewModelTests
{
    private static (AccentColorViewModel Vm, IAccentColorService Service) Create(string accent = "#2F6FEB")
    {
        IAccentColorService service = Substitute.For<IAccentColorService>();
        service.GetAccent().Returns(accent);
        service.DefaultAccent.Returns("#2F6FEB");
        return (new AccentColorViewModel(service), service);
    }

    [Fact]
    public void StartsOnTheRememberedColor_WithoutApplyingItAgain()
    {
        var (vm, service) = Create("#7C3AED");

        vm.Accent.Should().Be("#7C3AED");
        service.DidNotReceive().SetAccent(Arg.Any<string>());
    }

    [Fact]
    public void PickedColor_IsAppliedAtOnce()
    {
        var (vm, service) = Create();

        vm.Accent = "#16A34A";

        service.Received().SetAccent("#16A34A");
    }

    [Fact]
    public void Presets_AreDistinctColors()
    {
        var (vm, _) = Create();

        vm.Presets.Select(p => p.Hex).Should().OnlyHaveUniqueItems();
        vm.Presets.Should().Contain(p => p.Hex == vm.DefaultAccent);
    }
}
