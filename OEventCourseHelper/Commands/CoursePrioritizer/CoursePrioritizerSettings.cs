using Spectre.Console.Cli;
using System.ComponentModel;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class CoursePrioritizerSettings : IofXmlFileSettings
{
    [CommandOption("-w|--beam-width")]
    [Description("Sets the width of the beam for the search (must be a positive integer greater than zero).")]
    [DefaultValue(3)]
    public required int BeamWidth { get; init; } = 3;

    [CommandOption("-f|--filter")]
    [Description("One or more strings to filter course names by. Only courses containing one of these strings will be included")]
    public required string[] Filters { get; init; } = [];

    [CommandOption("--strict")]
    [Description("If set, the command will fail if any controls cannot be visited by the available courses. If not set the command will log a warning for each such control instead.")]
    public required bool Strict { get; init; } = false;

    [CommandOption("--porcelain [VERSION]")]
    [Description("Machine-readable output. Available versions: v1")]
    [DefaultValue("v1")]
    public required FlagValue<string> Porcelain { get; init; }
}
