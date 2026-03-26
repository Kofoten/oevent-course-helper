using Microsoft.Extensions.Logging;
using OEventCourseHelper.Cli;
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
    ApplicationContext applicationContext,
    ILogger<CoursePrioritizerCommand> logger)
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
            return ValidationResult.Error("Beam width must be a positive integer.");
        }

        if (settings.Porcelain.IsSet && !applicationContext.IsPorcelainVersionSupported(settings.Porcelain.Value))
        {
            return ValidationResult.Error($"Invalid porcelain version: {settings.Porcelain.Value}");
        }

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, CoursePrioritizerSettings settings, CancellationToken _)
    {
        if (settings.Porcelain.IsSet)
        {
            applicationContext.SetPorcelainLoggingMode(settings.Porcelain.Value);
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
        if (!ValidateDataSet(dataSet, settings.Strict, out var skippedControls))
        {
            return ExitCode.ValidationFailed;
        }

        var solver = new BeamSearchSolver(settings.BeamWidth);
        var result = solver.Solve(dataSet);
        if (!result.Success)
        {
            logger.NoSolutionFound();
            return ExitCode.NoSolutionFound;
        }

        var priority = 0;
        foreach (var prioritizedCourse in result.PriorityOrder)
        {
            priority++;
            logger.PriorityResult(priority, prioritizedCourse.CourseName, prioritizedCourse.IsRequired);
        }

        logger.PrioritizeSummary(
            dataSet.Courses.Length,
            result.CourseMask.Value.PopCount,
            dataSet.Controls.Length - skippedControls,
            dataSet.Controls.Length);

        return ExitCode.Success;
    }

    public bool ValidateDataSet(EventDataSet dataSet, bool strict, out int skippedControls)
    {
        skippedControls = 0;
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
            skippedControls++;
            logger.ControlSkippedWarning(dataSet.Controls[control]);
        }

        return true;
    }
}
