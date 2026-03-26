using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OEventCourseHelper.Logging.Porcelain;

/// <summary>
/// Formats the log entry inte a machine readable format: LogLevel:EventId|EventName\tparam1="value1",param2="value2"
/// </summary>
internal class V1PorcelainFormatter : IPorcelainFormatter
{
    public string Version { get; } = "v1";

    public void Write<TState>(in LogEntry<TState> logEntry, TextWriter textWriter)
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

                textWriter.Write($"{name}=\"");

                var rawValue = value?.ToString() ?? string.Empty;
                for (int i = 0; i < rawValue.Length; i++)
                {
                    switch (rawValue[i])
                    {
                        case '\r':
                            break;
                        case '\n':
                            textWriter.Write(' ');
                            break;
                        case '"':
                            textWriter.Write("\"\"");
                            break;
                        default:
                            textWriter.Write(rawValue[i]);
                            break;
                    }
                }

                textWriter.Write('"');
            }
        }

        textWriter.WriteLine();
    }
}
