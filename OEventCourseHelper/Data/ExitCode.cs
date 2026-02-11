namespace OEventCourseHelper.Data;

internal static class ExitCode
{
    public const int Success = 0;
    public const int UnhandledException = 1;
    public const int UnknownResult = 2;
    public const int UnexpectedErrorCode = 3;
    public const int FailedToParseArguments = 4;
    public const int FailedToLoadFile = 5;
    public const int NoSolutionFound = 6;
}
