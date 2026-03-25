using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Logging;

internal class OEventCourseHelperConsoleFormatter(IOptionsMonitor<OEventCourseHelperLoggingOptions> options)
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

    private static void WritePorcelain<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
    {
        textWriter.Write(logEntry.LogLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            LogLevel.None => "NON",
            _ => "NON"
        });

        textWriter.Write($":{logEntry.EventId.Id}");
        if (!string.IsNullOrWhiteSpace(logEntry.EventId.Name))
        {
            textWriter.Write($"|{logEntry.EventId.Name}");
        }

        textWriter.Write('\t');

        if (logEntry.State is IEnumerable<KeyValuePair<string, object>> properties)
        {
            bool first = true;
            foreach (var (name, value) in properties)
            {
                if (name == "{OriginalFormat}")
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    textWriter.Write(',');
                }

                var rawValue = value?.ToString() ?? string.Empty;
                var escapedValue = rawValue.Replace("\"", "\"\"");
                textWriter.Write($"{name}=\"{escapedValue}\"");
            }
        }

        textWriter.WriteLine();
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
