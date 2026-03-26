namespace OEventCourseHelper.Logging;

internal class OEventCourseHelperLoggingOptions
{
    public OEventCourseHelperLoggingMode LoggingMode { get; set; } =
        OEventCourseHelperLoggingMode.Spectre;

    public string PorcelainVersion { get; set; } = "v1";
}
