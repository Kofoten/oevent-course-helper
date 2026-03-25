using Microsoft.Extensions.Options;

namespace OEventCourseHelper.Tests.TestUtilities;

internal class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;

    public T Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        throw new NotImplementedException();
    }
}
