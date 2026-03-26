namespace OEventCourseHelper.Logging.Porcelain;

internal class PorcelainFormatterRegistry
{
    private readonly Dictionary<string, IPorcelainFormatter> formatters = [];

    public bool IsVersionSupported(string version) => formatters.ContainsKey(version);

    public IPorcelainFormatter GetFormatter(string version) => formatters[version];

    public bool Add(IPorcelainFormatter formatter)
        => formatters.TryAdd(formatter.Version, formatter);
}
