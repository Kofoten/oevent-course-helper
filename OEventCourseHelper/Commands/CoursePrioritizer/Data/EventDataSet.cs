using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record EventDataSet(int TotalEventControlCount, ImmutableArray<Course> Courses)
{
    public static EventDataSet Create(int totalEventControlCount, IEnumerable<Course.Builder> courseBuilders)
    {
        var bucketCount = BitMask.GetBucketCount(totalEventControlCount);
        var courses = courseBuilders
            .Select((x, i) => x.ToCourseMask(bucketCount, i))
            .ToImmutableArray();

        return new(totalEventControlCount, courses);
    }
}
