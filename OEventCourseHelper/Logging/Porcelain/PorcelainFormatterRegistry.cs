namespace OEventCourseHelper.Logging.Porcelain;

internal class PorcelainFormatterRegistry
{
    private readonly Dictionary<string, IPorcelainFormatter> formatters = [];

    public bool IsVersionSupported(string version) => formatters.ContainsKey(version);

    public IPorcelainFormatter GetFormatter(string version)
    {
        if (formatters.TryGetValue(version, out var formatter))
        {
            return formatter;
        }

        throw new NotSupportedException($"Porcelain version '{version}' is not supported.");
    }

    public bool Add(IPorcelainFormatter formatter)
        => formatters.TryAdd(formatter.Version, formatter);
}
