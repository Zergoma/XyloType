using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;

namespace XyloType.Application.Services;

public class ChartResponseTimeColorsBeginnerProvider : IChartResponseTimeColorsProvider
{
    virtual public string GetHexColorTimeResponse(double timeInSecondes, ThemeState themeState)
    {
        return themeState switch
        {
            ThemeState.Dark => timeInSecondes switch
            {
                < 1.0 => "#36B205",
                < 2.0 => "#91B200",
                < 3.0 => "#B25C00",
                < 4.0 => "#B21700",
                _ => "#444444"
            },
            _ => timeInSecondes switch
            {
                < 1.0 => "#42E506",
                < 2.0 => "#B5E200",
                < 3.0 => "#E07700",
                < 4.0 => "#DD2400",
                _ => "#000000"
            }
        };
    }

    public string GetHexColorBg(ThemeState themeState)
    {
        return themeState switch
        {
            ThemeState.Dark => "#1E2229",
            _ => "#FFFFFF",
        };
        
    }

    public string GetHexColorTxtLabel(ThemeState themeState)
    {
        return themeState switch
        {
            ThemeState.Dark => "#E5E7EB",
            _ => "#1F2937",
        };
    }
}


public class ChartResponseTimeColorIntermediateProvider : ChartResponseTimeColorsBeginnerProvider
{
    public override string GetHexColorTimeResponse(double timeInSecondes, ThemeState themeState)
    {
        return themeState switch
        {
            ThemeState.Dark => timeInSecondes switch
            {
                < 0.1 => "#36B205",
                < 0.2 => "#91B200",
                < 0.3 => "#B25C00",
                < 0.4 => "#B21700",
                _ => "#444444"
            },
            _ => timeInSecondes switch
            {
                < 0.1 => "#42E506",
                < 0.2 => "#B5E200",
                < 0.3 => "#E07700",
                < 0.4 => "#DD2400",
                _ => "#000000"
            }
        };
    }
}