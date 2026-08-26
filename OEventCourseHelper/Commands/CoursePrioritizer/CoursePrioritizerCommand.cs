using Kofoten.NativeCli;
using Microsoft.Extensions.Logging;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Data;
using OEventCourseHelper.Logging;
using System.Collections.Frozen;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class CoursePrioritizerCommand(
    ApplicationContext applicationContext,
    ILogger<CoursePrioritizerCommand> logger)
    : ICliCommand
{
    [CliArgument(0, nameof(IofXmlFilePath), Description = "The path to the IOF XML 3.0 file.")]
    public required string IofXmlFilePath { get; init; }

    [CliOption("beam-width", Short = 'b', Description = "The beam width to use for the prioritization algorithm. Must be a positive integer.")]
    public int BeamWidth { get; init; } = 3;

    [CliOption("filter", Short = 'f', Description = "One or more strings to filter course names by. Only courses containing one of these strings will be included.")]
    public FrozenSet<string> Filters { get; init; } = [];

    [CliOption("strict", Short = 's', Description = "If set, the prioritization will fail if any required courses are not included in the final result.")]
    public bool Strict { get; init; }

    [CliOption("porcelain", Description = "Machine-readable output. Available versions: v1", ImplicitValue = "v1")]
    public string? Porcelain { get; init; }

    public CliValidationResult Validate()
    {
        var errors = new List<string>();

        if (!File.Exists(IofXmlFilePath))
        {
            errors.Add($"The file '{IofXmlFilePath}' could not be found.");
        }

        if (BeamWidth <= 0)
        {
            errors.Add("Beam width must be a positive integer.");
        }

        if (Porcelain is not null && !applicationContext.IsPorcelainVersionSupported(Porcelain))
        {
            errors.Add($"Invalid porcelain version: {Porcelain}");
        }

        if (errors.Count > 0)
        {
            return new CliValidationResult.Failure(errors);
        }

        return new CliValidationResult.Success();
    }

    public int Execute()
    {
        if (Porcelain is not null)
        {
            applicationContext.SetPorcelainLoggingMode(Porcelain);
        }

        var engine = new CoursePrioritizerEngine(BeamWidth, Strict, Filters);

        CoursePrioritizerResult result;
        using (var fileStream = new FileStream(IofXmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            result = engine.Run(fileStream);
        }

        return result switch
        {
            CoursePrioritizerResult.ParseStreamFailure r => HandleParseStreamFailure(r, IofXmlFilePath),
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
