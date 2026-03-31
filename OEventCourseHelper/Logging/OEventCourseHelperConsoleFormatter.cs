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

    private static readonly Style traceStyle = new(Color.Silver, Color.Black);
    private static readonly Style debugStyle = new(Color.Aqua, Color.Black);
    private static readonly Style infoStyle = new(Color.Green3, Color.Black);
    private static readonly Style warningStyle = new(Color.Gold3, Color.Black);
    private static readonly Style errorStyle = new(Color.Red3, Color.Black);
    private static readonly Style criticalStyle = new(Color.Fuchsia, Color.Black);
    private static readonly Style noneStyle = new(Color.White, Color.Black);
    private static readonly Style eventIdStyle = new(Color.DarkSeaGreen3, Color.Black);
    private static readonly Style parameterHighlightingStyle = new(Color.LightSteelBlue, Color.Black);

    private readonly Lock ansiConsoleInitLock = new();
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

        switch (logEntry.LogLevel)
        {
            case LogLevel.Trace:
                ansiConsole.Write("TRACE", traceStyle);
                break;
            case LogLevel.Debug:
                ansiConsole.Write("DEBUG", debugStyle);
                break;
            case LogLevel.Information:
                ansiConsole.Write("INFO", infoStyle);
                break;
            case LogLevel.Warning:
                ansiConsole.Write("WARNING", warningStyle);
                break;
            case LogLevel.Error:
                ansiConsole.Write("ERROR", errorStyle);
                break;
            case LogLevel.Critical:
                ansiConsole.Write("CRITICAL", criticalStyle);
                break;
            case LogLevel.None:
            default:
                ansiConsole.Write("NONE", noneStyle);
                break;
        }

        ansiConsole.Write(": ");
        ansiConsole.Write(logEntry.EventId.Id.ToString(), eventIdStyle);

        if (!string.IsNullOrWhiteSpace(logEntry.EventId.Name))
        {
            ansiConsole.Write("|");
            ansiConsole.Write(logEntry.EventId.Name, eventIdStyle);
            ansiConsole.Write(": ");
        }

        if (logEntry.State is IEnumerable<KeyValuePair<string, object>> properties)
        {
            var propsDict = properties.ToDictionary(x => x.Key, x => x.Value);

            if (propsDict.TryGetValue("{OriginalFormat}", out var formatObj) && formatObj is string template)
            {
                if (template.StartsWith('{') && template.EndsWith('}') && template.Count(c => c == '{') == 1)
                {
                    ansiConsole.WriteLine(logEntry.Formatter.Invoke(logEntry.State, null));
                }
                else
                {
                    var lastIndex = 0;
                    while (true)
                    {
                        var openBrace = template.IndexOf('{', lastIndex);
                        if (openBrace == -1) break;

                        var closeBrace = template.IndexOf('}', openBrace);
                        if (closeBrace == -1) break;

                        ansiConsole.Write(template[lastIndex..openBrace]);
                        var paramName = template.Substring(openBrace + 1, closeBrace - openBrace - 1);
                        if (propsDict.TryGetValue(paramName, out var val))
                        {
                            ansiConsole.Write(val.ToString() ?? string.Empty, parameterHighlightingStyle);
                        }

                        lastIndex = closeBrace + 1;
                    }

                    if (lastIndex < template.Length)
                    {
                        ansiConsole.Write(template[lastIndex..]);
                    }

                    ansiConsole.WriteLine();
                }
            }
        }

        if (logEntry.Exception is not null)
        {
            ansiConsole.WriteException(logEntry.Exception);
        }
    }

    private void WritePorcelain<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
    {
        var formatter = porcelainFormatterRegistry.GetFormatter(options.CurrentValue.PorcelainVersion);
        formatter.Write(logEntry, textWriter);
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
