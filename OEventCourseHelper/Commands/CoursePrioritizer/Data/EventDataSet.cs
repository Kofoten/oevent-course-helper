using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class EventDataSet(int TotalEventControlCount, ImmutableArray<CourseMask> Courses)
{
    public static EventDataSet Create(int totalEventControlCount, IEnumerable<CourseMask.Builder> courseBuilders)
    {
        var bucketCount = BitMask.GetBucketCount(totalEventControlCount);
        var courses = courseBuilders
            .Select((x, i) => x.ToCourseMask(bucketCount, i))
            .ToImmutableArray();

        return new(totalEventControlCount, courses);
    }
}
