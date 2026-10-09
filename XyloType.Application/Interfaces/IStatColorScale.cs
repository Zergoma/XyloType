using XyloType.Application.Models.Themes;

namespace XyloType.Application.Interfaces;

/// <summary>
/// The colors of the result charts: from good (green) to bad (red), through amber.
/// </summary>
public interface IStatColorScale
{
    /// <param name="badness">From 0 (good) to 1 (bad), see <c>TypingTargets</c></param>
    string GetHexColor(double badness, ThemeState themeState);
}
