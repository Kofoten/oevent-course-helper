using GenerateThirdPartyNotices;
using Kofoten.NativeCli;

try
{
    return GenerateThirdPartyNoticesCommandParser.Parse(args).Execute();
}
catch (CliParseException cpe)
{
    Console.WriteLine($"Error parsing arguments: {cpe.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"Critical failure: {ex.Message}");
    return 42;
}
