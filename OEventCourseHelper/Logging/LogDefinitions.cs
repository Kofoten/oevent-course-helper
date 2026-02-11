using Microsoft.Extensions.Logging;

namespace OEventCourseHelper.Logging;

internal static partial class LogDefinitions
{
    [LoggerMessage(10000, LogLevel.Error, "Failed to parse arguments: {formattedErrors}")]
    public static partial void FailedToParseArguments(this ILogger logger, string formattedErrors);

    [LoggerMessage(10001, LogLevel.Error, "Failed to load the file: {filePath}{formattedErrors}")]
    public static partial void FailedToLoadFile(this ILogger logger, string filePath, string formattedErrors);

    [LoggerMessage(10002, LogLevel.Error, "There is no solution that will ensure that all controls will be visited.")]
    public static partial void NoSolutionFound(this ILogger logger);
}
