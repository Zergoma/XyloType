using FluentAssertions;

using NSubstitute;

using XyloType.Application.Interfaces;
using XyloType.ViewModels.Theme;

namespace XyloType.Tests.ViewModels;

public class AccentColorViewModelTests
{
    [Theory]
    [InlineData("#2f6feb", "#2F6FEB")]
    [InlineData("2F6FEB", "#2F6FEB")]
    [InlineData(" #abc ", "#AABBCC")]
    [InlineData("#12345", null)]
    [InlineData("#GGGGGG", null)]
    [InlineData("", null)]
    public void NormalizeHex_AcceptsTheUsualWritings(string text, string? expected)
        => ColorMath.NormalizeHex(text).Should().Be(expected);

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#2F6FEB")]
    [InlineData("#7C3AED")]
    [InlineData("#16A34A")]
    [InlineData("#808080")]
    public void HexAndHsv_GoBothWays(string hex)
    {
        var (hue, saturation, value) = ColorMath.HexToHsv(hex);

        ColorMath.HsvToHex(hue, saturation, value).Should().Be(hex);
    }

    private static (AccentColorViewModel Vm, IAccentColorService Service) Create(string accent = "#2F6FEB")
    {
        IAccentColorService service = Substitute.For<IAccentColorService>();
        service.GetAccent().Returns(accent);
        service.DefaultAccent.Returns("#2F6FEB");
        return (new AccentColorViewModel(service), service);
    }

    [Fact]
    public void Preset_IsAppliedAtOnce()
    {
        var (vm, service) = Create();

        vm.SelectPreset(vm.Presets.Single(p => p.Name == "Violet"));

        vm.Accent.Should().Be("#7C3AED");
        vm.HexText.Should().Be("#7C3AED");
        service.Received().SetAccent("#7C3AED");
    }

    [Fact]
    public void HexCode_IsAppliedOnlyWhenItIsAColor()
    {
        var (vm, service) = Create();

        vm.HexText = "#16A3";
        vm.IsHexValid.Should().BeFalse();
        service.DidNotReceive().SetAccent(Arg.Any<string>());

        vm.HexText = "#16A34A";
        vm.Accent.Should().Be("#16A34A");
        service.Received().SetAccent("#16A34A");
    }

    [Fact]
    public void PickedHueAndShade_GiveTheColor()
    {
        var (vm, service) = Create();

        vm.PickHue(0);
        vm.PickShade(1, 1);

        vm.Accent.Should().Be("#FF0000");
        vm.HueColor.Should().Be("#FF0000");
        service.Received().SetAccent("#FF0000");

        vm.ResetToDefault();
        vm.Accent.Should().Be("#2F6FEB");
    }
}
