using Microsoft.Extensions.Logging;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Data;
using OEventCourseHelper.Logging;
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

        var engine = new CoursePrioritizerEngine(settings.BeamWidth, settings.Strict, settings.Filters);

        CoursePrioritizerResult result;
        using (var fileStream = new FileStream(settings.IofXmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            result = engine.Run(fileStream);
        }

        return result switch
        {
            CoursePrioritizerResult.ParseStreamFailure r => HandleParseStreamFailure(r, settings.IofXmlFilePath),
            CoursePrioritizerResult.ValidationFailure r => HandleValidationFailure(r),
            CoursePrioritizerResult.NoSolutionFound r => HandleNoSolutionFound(r),
            CoursePrioritizerResult.Success r => HandleSuccess(r),
            _ => throw new NotImplementedException($"No handler exists for type: {result.GetType().Name}")
        };
    }

    private int HandleParseStreamFailure(CoursePrioritizerResult.ParseStreamFailure result, string iofXmlFilePath)
    {
        foreach (var error in result.Errors)
        {
            logger.IofSchemaViolation(error);
        }

        logger.FailedToLoadFile(iofXmlFilePath);
        return ExitCode.FailedToLoadFile;
    }

    private int HandleValidationFailure(CoursePrioritizerResult.ValidationFailure result)
    {
        LogSkippedControls(result.ValidationInfo.SkippedControls);
        logger.StrictModeValidationFailed(result.ValidationInfo.SkippedControls.Count);
        return ExitCode.ValidationFailed;
    }

    private int HandleNoSolutionFound(CoursePrioritizerResult.NoSolutionFound result)
    {
        LogSkippedControls(result.ValidationInfo.SkippedControls);
        logger.NoSolutionFound();
        return ExitCode.NoSolutionFound;
    }

    private int HandleSuccess(CoursePrioritizerResult.Success result)
    {
        LogSkippedControls(result.ValidationInfo.SkippedControls);

        var priority = 0;
        foreach (var prioritizedCourse in result.PriorityOrder)
        {
            priority++;
            logger.PriorityResult(priority, prioritizedCourse.CourseName, prioritizedCourse.IsRequired);
        }

        logger.PrioritizeSummary(
            result.Summary.TotalCourseCount,
            result.Summary.RequiredCourseCount,
            result.Summary.VisitedControlCount,
            result.Summary.TotalControlCount);

        return ExitCode.Success;
    }

    private void LogSkippedControls(IEnumerable<string> skippedControls)
    {
        foreach (var skippedControl in skippedControls)
        {
            logger.ControlSkippedWarning(skippedControl);
        }
    }
}
