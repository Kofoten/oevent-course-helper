using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;
using OEventCourseHelper.Core.Xml.Iof;

namespace OEventCourseHelper.Core.CoursePrioritizer;

public class CoursePrioritizerEngine(int BeamWidth, bool Strict, IEnumerable<string> Filters)
{
    public CoursePrioritizerResult Run(Stream iofXmlDataStream)
    {
        var filter = new CourseFilter(true, [.. Filters]);
        var dataSetReader = new EventDataSetNodeReader(filter);
        using (var reader = IOFXmlReader.Create(iofXmlDataStream, dataSetReader))
        {
            if (!reader.TryStream())
            {
                return new CoursePrioritizerResult.ParseStreamFailure(reader.Errors);
            }
        }

        var dataSet = dataSetReader.GetEventDataSet();
        if (!ValidateDataSet(dataSet, out var validationInfo))
        {
            return new CoursePrioritizerResult.ValidationFailure(validationInfo);
        }

        var solver = new BeamSearchSolver(BeamWidth);
        var result = solver.Solve(dataSet);
        if (!result.Success)
        {
            return new CoursePrioritizerResult.NoSolutionFound(validationInfo);
        }

        var summary = new Summary(
            dataSet.Courses.Length,
            result.CourseMask.Value.PopCount,
            dataSet.Controls.Length,
            dataSet.Controls.Length - validationInfo.SkippedControls.Count);

        return new CoursePrioritizerResult.Success(result.PriorityOrder, summary, validationInfo);
    }

    private bool ValidateDataSet(EventDataSet dataSet, out ValidationInfo validationInfo)
    {
        var orphanedControlsMaskBuilder = BitMask.Builder.From(BitMask.Fill(dataSet.Controls.Length));
        foreach (var course in dataSet.Courses)
        {
            orphanedControlsMaskBuilder.AndNot(course.ControlMask);
        }

        var orphanedControlsMask = orphanedControlsMaskBuilder.ToBitMask();
        if (orphanedControlsMask.IsZero)
        {
            validationInfo = new([]);
            return true;
        }

        var skippedControls = new List<string>();
        foreach (var control in orphanedControlsMask)
        {
            skippedControls.Add(dataSet.Controls[control]);
        }

        validationInfo = new(skippedControls);
        return !Strict; // Should return False if Strict is True; otherwise True.
    }

    public record ValidationInfo(IReadOnlyList<string> SkippedControls);

    public record Summary(int TotalCourseCount, int RequiredCourseCount, int TotalControlCount, int VisitedControlCount);
}
