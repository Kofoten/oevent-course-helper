using Microsoft.Extensions.Logging;

namespace OEventCourseHelper.TestUtilities;

public class FakeLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId Id, string Message)> Logs { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logs.Add((logLevel, eventId, formatter(state, exception)));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
