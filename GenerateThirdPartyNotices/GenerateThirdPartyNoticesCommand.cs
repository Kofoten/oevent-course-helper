using Kofoten.NativeCli;
using System.Collections.Frozen;
using System.Text.Json;

namespace GenerateThirdPartyNotices;

internal class GenerateThirdPartyNoticesCommand : ICliCommand
{
    private const string LibrariesPropertyName = "libraries";
    private const string PackageFoldersPropertyName = "packageFolders";
    private const string LibraryTypePropertyName = "type";
    private const string LibraryPathPropertyName = "path";
    private const string LibraryLicenseUrlPropertyName = "licenseUrl";

    private const string PackageLibraryType = "package";

    [CliArgument(0, nameof(AssetsJsonPath), Description = "The path to the assets.json file.")]
    public required string AssetsJsonPath { get; init; }

    [CliArgument(1, nameof(ThirdPartyNoticesOutputPath), Description = "The path to the output file for the third-party notices.")]
    public required string ThirdPartyNoticesOutputPath { get; init; }

    [CliArgument(2, nameof(FallbackLicensesDirectoryPath), Description = "The path to the directory containing fallback licenses.")]
    public required string FallbackLicensesDirectoryPath { get; init; }

    [CliOption("ignore-packages", Short = 'i', Description = "One or more package names to ignore when generating third-party notices.")]
    public FrozenSet<string> IgnorePackages { get; init; } = [];

    public CliValidationResult Validate()
    {
        var errors = new List<string>();
        if (!File.Exists(AssetsJsonPath))
        {
            errors.Add($"The file '{AssetsJsonPath}' could not be found.");
        }
        if (!Directory.Exists(FallbackLicensesDirectoryPath))
        {
            errors.Add($"The directory '{FallbackLicensesDirectoryPath}' could not be found.");
        }
        if (errors.Count > 0)
        {
            return new CliValidationResult.Failure(errors);
        }
        return new CliValidationResult.Success();
    }

    public int Execute()
    {
        var errors = new List<string>();
        using var document = JsonDocument.Parse(File.ReadAllText(AssetsJsonPath));
        var root = document.RootElement;

        var libraries = root.GetProperty(LibrariesPropertyName);
        var packageFolders = root.GetProperty(PackageFoldersPropertyName)
            .EnumerateObject()
            .Select(x => x.Name)
            .ToList();

        using var writer = new StreamWriter(ThirdPartyNoticesOutputPath);
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

            var fallbackPath = Path.Combine(FallbackLicensesDirectoryPath, library.Name.Replace('/', '_'));
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
            else if (!IgnorePackages.Contains(library.Name[..library.Name.IndexOf('/')]))
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

        Console.WriteLine($"Successfully generated {ThirdPartyNoticesOutputPath}");
        return 0;
    }
}
