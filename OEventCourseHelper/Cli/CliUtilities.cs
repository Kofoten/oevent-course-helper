using Microsoft.Extensions.Logging;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OEventCourseHelper.Cli;

internal class CliUtilities
{
    public static int ExceptionHandler(Exception exception, ITypeResolver? resolver)
    {
        ILogger<Program>? logger = null;
        try
        {
            logger = resolver?.Resolve<ILogger<Program>>();
        }
        catch { }

        if (logger is not null)
        {
            logger.LogCritical(exception, "An unexpected error occurred.");
        }
        else
        {
            AnsiConsole.WriteException(exception);
        }

        return ExitCode.UnhandledException;
    }
}
