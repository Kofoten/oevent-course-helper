using System.Text.Json;

const string LibrariesPropertyName = "libraries";
const string PackageFoldersPropertyName = "packageFolders";
const string LibraryTypePropertyName = "type";
const string LibraryPathPropertyName = "path";
const string LibraryLicenseUrlPropertyName = "licenseUrl";

const string PackageLibraryType = "package";

if (args.Length < 3)
{
    Console.WriteLine("Usage: GenerateThirdPartyNotices <input_assets_json> <output_notices_file> <fallback_licence_lookup_directory>");
    return 1;
}

string assetsPath = args[0];
string outputPath = args[1];
string fallbackDirectory = args[2];

if (!File.Exists(assetsPath))
{
    Console.WriteLine($"Error: Assets file not found at {assetsPath}");
    return 1;
}

if (!Directory.Exists(fallbackDirectory))
{
    Console.WriteLine($"Error: Fallback directory does not exist");
    return 1;
}

try
{
    var errors = new List<string>();
    using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
    var root = document.RootElement;

    var libraries = root.GetProperty(LibrariesPropertyName);
    var packageFolders = root.GetProperty(PackageFoldersPropertyName)
        .EnumerateObject()
        .Select(x => x.Name)
        .ToList();

    using var writer = new StreamWriter(outputPath);
    writer.WriteLine("THIRD-PARTY NOTICES");
    writer.WriteLine("===================");
    writer.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    writer.WriteLine();

    foreach (var library in libraries.EnumerateObject())
    {
        var libraryType = library.Value.GetProperty(LibraryTypePropertyName).GetString();
        if (libraryType != PackageLibraryType)
        {
            continue;
        }

        var relativePath = library.Value.GetProperty(LibraryPathPropertyName).GetString();
        string? fullPath = packageFolders
            .Select(folder =>
            {
                if (relativePath is null)
                {
                    return folder;
                }

                return Path.Combine(folder, relativePath);
            })
            .FirstOrDefault(Directory.Exists);

        if (string.IsNullOrEmpty(fullPath))
        {
            continue;
        }

        writer.WriteLine($"Package: {library.Name}");
        writer.WriteLine(new string('-', library.Name.Length + 9));

        var fallbackPath = Path.Combine(fallbackDirectory, library.Name.Replace('/', '_'));
        var licenseFile = Directory.EnumerateFiles(fullPath, "LICENSE*", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (licenseFile is not null)
        {
            writer.WriteLine(File.ReadAllText(licenseFile));
        }
        else if (File.Exists(fallbackPath))
        {
            writer.WriteLine(File.ReadAllText(fallbackPath));
        }
        else if (library.Value.TryGetProperty(LibraryLicenseUrlPropertyName, out var url))
        {
            writer.WriteLine($"License text not found in package. Refer to: {url.GetString()}");
        }
        else
        {
            errors.Add($"No license information found for package '{library.Name}'.");
        }

        writer.WriteLine();
        writer.WriteLine(new string('=', 60));
        writer.WriteLine();
    }

    if (errors.Count > 0)
    {
        Console.WriteLine("Error: Missing licenses.");
        foreach (var error in errors)
        {
            Console.WriteLine($"\t{error}");
        }

        return 1;
    }

    Console.WriteLine($"Successfully generated {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Critical failure: {ex.Message}");
    return 1;
}