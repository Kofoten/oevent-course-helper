using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OEventCourseHelper.Data;

internal record Options(
    string IOFXmlFilePath,
    bool Help,
    int BeamWidth,
    FrozenSet<string> Filters)
{
    public const int DefaultBeamWidth = 3;

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out Options? options,
        [NotNullWhen(false)] out List<string>? errors)
    {
        errors = [];

        if (args.Length == 0 || args.Any(x => x == "-h" || x == "--help"))
        {
            options = new Options(string.Empty, true, DefaultBeamWidth, FrozenSet<string>.Empty);
            return true;
        }

        int beamWidth = DefaultBeamWidth;
        var filters = new HashSet<string>();

        string? currentKey = null;
        var parsedKeys = new HashSet<string>();
        for (int i = 1; i < args.Length; i++)
        {
            var currentValue = args[i];
            string? key = currentValue switch
            {
                "-w" => nameof(BeamWidth),
                "--beam-width" => nameof(BeamWidth),
                "-f" => nameof(Filters),
                "--filters" => nameof(Filters),
                _ => null,
            };

            if (key is not null)
            {
                if (!parsedKeys.Add(key))
                {
                    errors.Add($"The option {currentValue} (key: {key}) has been specified multiple times.");
                }

                currentKey = key;
                continue;
            }

            if (currentKey is null)
            {
                errors.Add($"The value '{currentValue}' at position {i} does not correspond to any option.");
            }

            switch (currentKey)
            {
                case nameof(BeamWidth):
                    if (!int.TryParse(currentValue, out beamWidth) || beamWidth < 1)
                    {
                        errors.Add($"The beam width must be a positive integer.");
                    }
                    currentKey = null; // Reset the key to make sure an error is detected if mutiple values are supplied.
                    break;
                case nameof(Filters):
                    filters.Add(currentValue); // Do NOT reset key since this is a multi value option.
                    break;
                default:
                    break;
            };
        }

        options = new Options(args[0], false, beamWidth, filters.ToFrozenSet());
        return errors.Count == 0;
    }

    public static string HelpText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Description:");
        builder.AppendLine("  Finds the optimal order of courses to visit all controls using a beam search algorithm.");
        builder.AppendLine();
        builder.AppendLine("Usage:");
        builder.AppendLine("  OEventCourseHelper <IOFXmlFilePath> [options]");
        builder.AppendLine();
        builder.AppendLine("Arguments:");
        builder.AppendLine("  <IOFXmlFilePath>    The path to the IOF XML 3.0 file. (Required)");
        builder.AppendLine();
        builder.AppendLine("Options:");
        builder.AppendLine("  -w, --beam-width <int>    Sets the width of the beam for the search.");
        builder.AppendLine("                            [Default: 3]");
        builder.AppendLine("  -f, --filters <string>    One or more strings to filter course names by. Only");
        builder.AppendLine("                            courses containing one of these strings will be");
        builder.AppendLine("                            included.");
        builder.AppendLine("  -h, --help                Show this help message and exit.");
        return builder.ToString();
    }
}
