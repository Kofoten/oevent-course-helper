using Microsoft.Extensions.Logging;
using OEventCourseHelper;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<Program>();

if (!Options.TryParse(args, out var options, out var errors))
{
    logger.FailedToParseArguments(errors.FormatErrors());
    return ExitCode.FailedToParseArguments;
}

if (options.Help)
{
    Console.Write(Options.HelpText());
    return ExitCode.Success;
}

var runtime = new Runtime(options, logger);
try
{
    var result = runtime.Run();
    return result switch
    {
        Success<CourseResult[], ErrorCode> success => HandleSuccess(success),
        Failure<CourseResult[], ErrorCode> failure => HandleFailure(failure),
        _ => ExitCode.UnknownResult,
    };
}
catch (Exception ex)
{
    logger.LogCritical(ex, "An unexpected error occurred.");
    return ExitCode.UnhandledException;
}

static int HandleSuccess(Success<CourseResult[], ErrorCode> success)
{
    foreach (var result in success.Value)
    {
        var suffix = result.IsRequired ? " (required)" : string.Empty;
        Console.WriteLine($"{result.Name}{suffix}");
    }

    return ExitCode.Success;
}

static int HandleFailure(Failure<CourseResult[], ErrorCode> failure)
{
    return failure.Error switch
    {
        ErrorCode.FailedToLoadFile => ExitCode.FailedToLoadFile,
        ErrorCode.NoSolutionFound => ExitCode.NoSolutionFound,
        _ => ExitCode.UnexpectedErrorCode,
    };
}
