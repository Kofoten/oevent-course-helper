using Microsoft.Extensions.Logging;
using OEventCourseHelper.Commands.CoursePrioritizer.IO;
using OEventCourseHelper.Commands.CoursePrioritizer.Solvers;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Xml.Iof;
using Spectre.Console;
using Spectre.Console.Cli;

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
        var filter = new CourseMaskBuilderFilter(true, [.. settings.Filters]);
        var courseReader = new CourseMaskNodeReader(filter);
        var iofReader = IOFXmlReader.Create();
        if (!iofReader.TryStream(settings.IofXmlFilePath, courseReader, out var errors))
        {
            logger.FailedToLoadFile(settings.IofXmlFilePath, errors.FormatErrors());
            return ExitCode.FailedToLoadFile;
        }

        var solver = new BitmaskBeamSearchSolver(settings.BeamWidth);
        var solverContext = courseReader.GetBitmaskBeamSearchSolverContext();
        if (!solver.TrySolve(solverContext, out var result))
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
