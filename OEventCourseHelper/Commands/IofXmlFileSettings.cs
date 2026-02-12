using Spectre.Console.Cli;
using System.ComponentModel;

namespace OEventCourseHelper.Commands;

internal class IofXmlFileSettings : CommandSettings
{
    [CommandArgument(0, "<IOFXmlFilePath>")]
    [Description("The path to the IOF XML 3.0 file")]
    public required string IofXmlFilePath { get; init; }
}
