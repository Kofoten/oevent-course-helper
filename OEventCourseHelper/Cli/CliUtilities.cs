using Kofoten.NativeCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OEventCourseHelper.Data;
using OEventCourseHelper.Logging;

namespace OEventCourseHelper.Cli;

internal class CliUtilities
{
    public static int ExceptionHandler(Exception exception, IServiceProvider? sp)
    {
        ILogger<Program>? logger = sp?.GetService<ILogger<Program>>();

        if (exception is CliParseException cpe)
        {
            if (logger is not null)
            {
                logger.FailedToParseArguments(cpe.Message);
            }
            else
            {
                Console.Out.WriteLine(cpe.Message);
            }

            return ExitCode.FailedToParseArguments;
        }

        if (logger is not null)
        {
            logger.UnhandledException(exception);
        }
        else
        {
            TerminalWriter.Create().WriteException(Console.Error, exception);
        }

        return ExitCode.UnhandledException;
    }
}
