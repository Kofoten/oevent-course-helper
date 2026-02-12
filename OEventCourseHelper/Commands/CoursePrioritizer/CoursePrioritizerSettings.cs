using Spectre.Console.Cli;
using System.ComponentModel;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class CoursePrioritizerSettings : IofXmlFileSettings
{
    [CommandOption("-w|--beam-width")]
    [Description("Sets the width of the beam for the search")]
    [DefaultValue(3)]
    public int BeamWidth { get; init; } = 3;

    [CommandOption("-f|--filter")]
    [Description("One or more strings to filter course names by. Only courses containing one of these strings will be included")]
    public string[] Filters { get; init; } = [];
}
