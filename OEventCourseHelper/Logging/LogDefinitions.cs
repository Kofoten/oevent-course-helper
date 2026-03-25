using Microsoft.Extensions.Logging;

namespace OEventCourseHelper.Logging;

internal static partial class LogDefinitions
{
    #region General (10000 - 10999)
    [LoggerMessage(10000, LogLevel.Error, "Failed to load the file: {filePath}")]
    public static partial void FailedToLoadFile(this ILogger logger, string filePath);

    [LoggerMessage(10001, LogLevel.Error, "{message}")]
    public static partial void IofSchemaViolation(this ILogger logger, string message);
    #endregion

    #region CoursePrioritizer (11000 - 11999)
    [LoggerMessage(11000, LogLevel.Error, "There is no solution that will ensure that all controls will be visited.")]
    public static partial void NoSolutionFound(this ILogger logger);

    [LoggerMessage(11001, LogLevel.Warning, "Control '{controlId}' cannot be visited by any of the available courses.")]
    public static partial void ControlSkippedWarning(this ILogger logger, string controlId);

    [LoggerMessage(11002, LogLevel.Error, "Strict mode is enabled and {count} control(s) cannot be visited. Aborting.")]
    public static partial void StrictModeValidationFailed(this ILogger logger, int count);

    [LoggerMessage(11003, LogLevel.Information, "{priority}. {courseName} (required: {required})")]
    public static partial void PriorityResult(this ILogger logger, int priority, string courseName, bool required);
    #endregion
}
