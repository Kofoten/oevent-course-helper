namespace OEventCourseHelper.Logging.Porcelain;

internal class PorcelainFormatterRegistry(IEnumerable<IPorcelainFormatter> formatters)
{
    private readonly Dictionary<string, IPorcelainFormatter> formatters = formatters.ToDictionary(x => x.Version);

    public bool IsVersionSupported(string version) => formatters.ContainsKey(version);

    public IPorcelainFormatter GetFormatter(string version)
    {
        if (formatters.TryGetValue(version, out var formatter))
        {
            return formatter;
        }

        throw new NotSupportedException($"Porcelain version '{version}' is not supported.");
    }
}
