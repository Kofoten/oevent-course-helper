using Microsoft.Extensions.Logging;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Logging;
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

        if (exception is CommandRuntimeException cre)
        {
            if (logger is not null)
            {
                logger.FailedToParseArguments(cre.Message);
            }
            else
            {
                AnsiConsole.WriteLine(cre.Message);
            }

            return ExitCode.FailedToParseArguments;
        }

        if (logger is not null)
        {
            logger.UnhandledException(exception);
        }
        else
        {
            AnsiConsole.WriteException(exception);
        }

        return ExitCode.UnhandledException;
    }
}
