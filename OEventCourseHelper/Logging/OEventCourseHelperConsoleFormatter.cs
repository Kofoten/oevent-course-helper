using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OEventCourseHelper.Logging.Porcelain;

namespace OEventCourseHelper.Logging;

internal class OEventCourseHelperConsoleFormatter(
    IOptionsMonitor<OEventCourseHelperLoggingOptions> options,
    PorcelainFormatterRegistry porcelainFormatterRegistry)
    : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "oevent-course-helper-console-formatter";

    private readonly IOptionsMonitor<OEventCourseHelperLoggingOptions> options = options;
    private readonly TerminalWriter terminalWriter = TerminalWriter.Create();

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
        switch (logEntry.LogLevel)
        {
            case LogLevel.Trace:
                terminalWriter.WriteTraceLabel(textWriter);
                break;
            case LogLevel.Debug:
                terminalWriter.WriteDebugLabel(textWriter);
                break;
            case LogLevel.Information:
                terminalWriter.WriteInfoLabel(textWriter);
                break;
            case LogLevel.Warning:
                terminalWriter.WriteWarningLabel(textWriter);
                break;
            case LogLevel.Error:
                terminalWriter.WriteErrorLabel(textWriter);
                break;
            case LogLevel.Critical:
                terminalWriter.WriteCriticalLabel(textWriter);
                break;
            case LogLevel.None:
            default:
                terminalWriter.WriteNoneLabel(textWriter);
                break;
        }

        textWriter.Write(": ");
        terminalWriter.WriteEventId(textWriter, logEntry.EventId);
        textWriter.Write(": ");

        if (logEntry.State is IEnumerable<KeyValuePair<string, object>> properties)
        {
            var propsDict = properties.ToDictionary(x => x.Key, x => x.Value);

            if (propsDict.TryGetValue("{OriginalFormat}", out var formatObj) && formatObj is string template)
            {
                if (template.StartsWith('{') && template.EndsWith('}') && template.Count(c => c == '{') == 1)
                {
                    textWriter.WriteLine(logEntry.Formatter.Invoke(logEntry.State, null));
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

                        textWriter.Write(template[lastIndex..openBrace]);
                        var paramName = template.Substring(openBrace + 1, closeBrace - openBrace - 1);
                        if (propsDict.TryGetValue(paramName, out var val))
                        {
                            terminalWriter.WriteParameter(textWriter, val?.ToString() ?? string.Empty);
                        }

                        lastIndex = closeBrace + 1;
                    }

                    if (lastIndex < template.Length)
                    {
                        textWriter.Write(template[lastIndex..]);
                    }

                    textWriter.WriteLine();
                }
            }
        }

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine();
            terminalWriter.WriteException(textWriter, logEntry.Exception);
        }
    }

    private void WritePorcelain<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
    {
        var formatter = porcelainFormatterRegistry.GetFormatter(options.CurrentValue.PorcelainVersion);
        formatter.Write(logEntry, textWriter);
    }
}
