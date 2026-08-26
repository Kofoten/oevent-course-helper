namespace OEventCourseHelper.Logging;

public enum ColorSupportLevel
{
    /// <summary>
    /// Terminals that don't support ANSI, or user requested NO_COLOR
    /// </summary>
    None,

    /// <summary>
    /// Fallback/Legacy
    /// </summary>
    Basic16,

    /// <summary>
    /// Terminal ends in '256color'
    /// </summary>
    Palette256,

    /// <summary>
    /// COLORTERM = truecolor
    /// </summary>
    TrueColor
}
