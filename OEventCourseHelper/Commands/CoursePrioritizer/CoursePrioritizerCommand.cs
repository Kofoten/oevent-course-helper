using Microsoft.Extensions.Logging;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Xml;
using OEventCourseHelper.Xml.NodeReaders;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Frozen;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class CoursePrioritizerCommand(ILogger<CoursePrioritizerCommand> logger) : Command<CoursePrioritizerSettings>
{
    public override ValidationResult Validate(CommandContext context, CoursePrioritizerSettings settings)
    {
        if (!File.Exists(settings.IofXmlFilePath))
        {
            return ValidationResult.Error($"The file '{settings.IofXmlFilePath}' could not be found.");
        }

        if (settings.BeamWidth <= 0)
        {
            return ValidationResult.Error("Beam width must be positive.");
        }

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, CoursePrioritizerSettings settings, CancellationToken _)
    {
        var iofReader = IOFXmlReader.Create();
        var courseReader = new CourseNodeReader();
        if (!iofReader.TryStream(settings.IofXmlFilePath, courseReader, out var errors))
        {
            logger.FailedToLoadFile(settings.IofXmlFilePath, errors.FormatErrors());
            return ExitCode.FailedToLoadFile;
        }

        var filteredCourses = courseReader.Courses
            .Where(x => x.Controls.Count > 0)
            .Where(x => settings.Filters.Length == 0 || settings.Filters.Any(y => x.Name.Contains(y)))
            .ToFrozenSet();

        var solver = new BeamSearchSolver(settings.BeamWidth);
        if (!solver.TrySolve(filteredCourses, out var result))
        {
            logger.NoSolutionFound();
            return ExitCode.NoSolutionFound;
        }

        foreach (var course in result)
        {
            var suffix = course.IsRequired ? " (required)" : string.Empty;
            Console.WriteLine($"{course.Name}{suffix}");
        }

        return ExitCode.Success;
    }
}
