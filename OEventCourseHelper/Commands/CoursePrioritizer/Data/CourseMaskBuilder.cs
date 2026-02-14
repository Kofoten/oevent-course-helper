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
            if (i < ControlMask.Count)
            {
                maskBuilder.Add(ControlMask[i]);
            }
            else
            {
                maskBuilder.Add(0UL);
            }
        }

        return new CourseMask(
            CourseName,
            maskBuilder.DrainToImmutable(),
            ControlCount);
    }
}
