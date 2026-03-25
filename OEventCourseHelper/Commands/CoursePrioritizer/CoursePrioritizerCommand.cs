using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Commands.CoursePrioritizer.IO;
using OEventCourseHelper.Commands.CoursePrioritizer.Solvers;
using OEventCourseHelper.Data;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Xml.Iof;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class CoursePrioritizerCommand(
    ILogger<CoursePrioritizerCommand> logger,
    IOptionsMonitor<OEventCourseHelperLoggingOptions> loggingOptions)
    : Command<CoursePrioritizerSettings>
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
        if (settings.Porcelain)
        {
            loggingOptions.CurrentValue.LoggingMode = OEventCourseHelperLoggingMode.Porcelain;
        }

        var filter = new CourseFilter(true, [.. settings.Filters]);
        var dataSetReader = new EventDataSetNodeReader(filter);
        var iofReader = IOFXmlReader.Create();
        if (!iofReader.TryStream(settings.IofXmlFilePath, dataSetReader, out var errors))
        {
            foreach (var error in errors)
            {
                logger.IofSchemaViolation(error);
            }

            logger.FailedToLoadFile(settings.IofXmlFilePath);
            return ExitCode.FailedToLoadFile;
        }

        var dataSet = dataSetReader.GetEventDataSet();
        if (!ValidateDataSet(dataSet, settings.Strict))
        {
            return ExitCode.ValidationFailed;
        }

        var solver = new BeamSearchSolver(settings.BeamWidth);
        if (!solver.TrySolve(dataSet, out var result))
        {
            logger.NoSolutionFound();
            return ExitCode.NoSolutionFound;
        }

        for (int i = 0; i < result.Length; i++)
        {
            logger.PriorityResult(i + 1, result[i].CourseName, result[i].IsRequired);
        }

        return ExitCode.Success;
    }

    public bool ValidateDataSet(EventDataSet dataSet, bool strict)
    {
        var orphanedControlsMaskBuilder = BitMask.Builder.From(BitMask.Fill(dataSet.Controls.Length));
        foreach (var course in dataSet.Courses)
        {
            orphanedControlsMaskBuilder.AndNot(course.ControlMask);
        }

        var orphanedControlsMask = orphanedControlsMaskBuilder.ToBitMask();
        if (orphanedControlsMask.IsZero)
        {
            return true;
        }

        if (strict)
        {
            logger.StrictModeValidationFailed(orphanedControlsMask.PopCount);
            return false;
        }

        foreach (var control in orphanedControlsMask)
        {
            logger.ControlSkippedWarning(dataSet.Controls[control]);
        }

        return true;
    }
}
