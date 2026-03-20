using System.Text.Json;

#nullable enable

if (Args.Count < 2)
{
    Console.WriteLine("Usage: LicenseGenerator <input_assets_json> <output_notices_file>");
    return 1;
}

string assetsPath = Args[0];
string outputPath = Args[1];

if (!File.Exists(assetsPath))
{
    Console.WriteLine($"Error: Assets file not found at {assetsPath}");
    return 1;
}

try
{
    using var jDoc = JsonDocument.Parse(File.ReadAllText(assetsPath));
    var root = jDoc.RootElement;

    // Source of Truth: Find where NuGet stored the bits during 'dotnet restore'
    var packageFolders = root.GetProperty("packageFolders").EnumerateObject().Select(x => x.Name).ToList();
    var libraries = root.GetProperty("libraries");

    using var writer = new StreamWriter(outputPath);
    writer.WriteLine("THIRD-PARTY NOTICES");
    writer.WriteLine("===================");
    writer.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n");

    foreach (var library in libraries.EnumerateObject())
    {
        // Only audit actual NuGet packages, ignoring local project references
        if (library.Value.GetProperty("type").GetString() != "package") continue;

        var relativePath = library.Value.GetProperty("path").GetString();

        // Search all package folders defined in assets.json
        string? fullPath = packageFolders
            .Select(folder => relativePath is null ? folder : Path.Combine(folder, relativePath))
            .FirstOrDefault(Directory.Exists);

        if (string.IsNullOrEmpty(fullPath)) continue;

        writer.WriteLine($"Package: {library.Name}");
        writer.WriteLine(new string('-', library.Name.Length + 9));

        // Search for license files (LICENSE, LICENSE.txt, LICENSE.md, etc.)
        var licenseFile = Directory.EnumerateFiles(fullPath, "LICENSE*", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (licenseFile != null)
        {
            writer.WriteLine(File.ReadAllText(licenseFile));
        }
        else if (library.Value.TryGetProperty("licenseUrl", out var url))
        {
            writer.WriteLine($"License text not found in package. Refer to: {url.GetString()}");
        }

        writer.WriteLine("\n" + new string('=', 60) + "\n");
    }

    Console.WriteLine($"Successfully generated {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Critical failure: {ex.Message}");
    return 1;
}