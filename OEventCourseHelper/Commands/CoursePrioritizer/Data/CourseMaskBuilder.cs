using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class CourseMaskBuilder()
{
    public string CourseName { get; set; } = "Unknown Course";
    public IList<ulong> ControlMask { get; set; } = [];
    public int ControlCount { get; set; } = 0;

    public CourseMask ToCourseMask(int bucketCount)
    {
        var maskBuilder = ImmutableArray.CreateBuilder<ulong>(bucketCount);
        for (int i = 0; i < bucketCount; i++)
        {
            maskBuilder.Add(ControlMask[i]);
        }

        return new CourseMask(
            CourseName,
            maskBuilder.DrainToImmutable(),
            ControlCount);
    }
}
