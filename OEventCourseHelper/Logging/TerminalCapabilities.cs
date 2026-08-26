namespace OEventCourseHelper.Logging;

internal static class TerminalCapabilities
{
    public static ColorSupportLevel GetSupportedPalette()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
        {
            return ColorSupportLevel.None;
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION")))
        {
            return ColorSupportLevel.TrueColor;
        }

        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        if (colorTerm == "truecolor" || colorTerm == "24bit")
        {
            return ColorSupportLevel.TrueColor;
        }

        var term = Environment.GetEnvironmentVariable("TERM");
        if (!string.IsNullOrEmpty(term) && term.Contains("256color", StringComparison.OrdinalIgnoreCase))
        {
            return ColorSupportLevel.Palette256;
        }

        return ColorSupportLevel.Basic16;
    }
}
