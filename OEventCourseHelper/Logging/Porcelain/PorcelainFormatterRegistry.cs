using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Logging.Porcelain;

internal class PorcelainFormatterRegistry
{
    private readonly Dictionary<string, IPorcelainFormatter> formatters = [];

    public bool IsVersionSupported(string version) => formatters.ContainsKey(version);

    public bool TryGetFormatter(string version, [MaybeNullWhen(false)] out IPorcelainFormatter formatter)
        => formatters.TryGetValue(version, out formatter);

    public bool Add(IPorcelainFormatter formatter)
        => formatters.TryAdd(formatter.Version, formatter);
}
