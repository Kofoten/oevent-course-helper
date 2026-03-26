using Microsoft.Extensions.Logging.Abstractions;

namespace OEventCourseHelper.Logging.Porcelain;

internal interface IPorcelainFormatter
{
    public string Version { get; }

    public void Write<TState>(in LogEntry<TState> logEntry, TextWriter textWriter);
}
