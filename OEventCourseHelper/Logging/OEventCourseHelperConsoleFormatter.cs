using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OEventCourseHelper.Logging.Porcelain;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Logging;

internal class OEventCourseHelperConsoleFormatter(
    IOptionsMonitor<OEventCourseHelperLoggingOptions> options,
    PorcelainFormatterRegistry porcelainFormatterRegistry)
    : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "oevent-course-helper-console-formatter";

    private readonly IOptionsMonitor<OEventCourseHelperLoggingOptions> options = options;

    private readonly object ansiConsoleInitLock = new();
    private IAnsiConsole? ansiConsole = null;

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        switch (options.CurrentValue.LoggingMode)
        {
            case OEventCourseHelperLoggingMode.Spectre:
                WriteSpectre(logEntry, textWriter);
                break;
            case OEventCourseHelperLoggingMode.Porcelain:
                WritePorcelain(logEntry, textWriter);
                break;
            default:
                WriteSpectre(logEntry, textWriter);
                break;
        }
    }

    private void WriteSpectre<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
    {
        EnsureAnsiConsole(textWriter);
        var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
        ansiConsole.WriteLine(message);
    }

    private void WritePorcelain<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
    {
        if (porcelainFormatterRegistry.TryGetFormatter(options.CurrentValue.PorcelainVersion, out var formatter))
        {
            formatter.Write(logEntry, textWriter);
        }
        else
        {
            textWriter.WriteLine(logEntry.Formatter.Invoke(logEntry.State, null));
        }
    }

    [MemberNotNull(nameof(ansiConsole))]
    private void EnsureAnsiConsole(TextWriter textWriter)
    {
        if (ansiConsole is not null)
        {
            return;
        }

        lock (ansiConsoleInitLock)
        {
            if (ansiConsole is not null)
            {
                return;
            }

            var settings = new AnsiConsoleSettings()
            {
                Out = new AnsiConsoleOutput(textWriter),
            };

            ansiConsole ??= AnsiConsole.Create(settings);
        }
    }
}
