using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// A data set containing event data.
/// </summary>
/// <param name="TotalEventControlCount">The total number of controls in the event.</param>
/// <param name="Courses">The courses in the event.</param>
internal record EventDataSet(int TotalEventControlCount, ImmutableArray<Course> Courses)
{
    /// <summary>
    /// Creates a new instance of <see cref="EventDataSet"/> from <paramref name="courseBuilders"/>.
    /// </summary>
    /// <param name="totalEventControlCount">The total number of controls in the event.</param>
    /// <param name="courseBuilders">The builders for the courses in the event.</param>
    /// <returns>A new instance of <see cref="EventDataSet"/>.</returns>
    public static EventDataSet Create(int totalEventControlCount, IEnumerable<Course.Builder> courseBuilders)
    {
        var bucketCount = BitMask.GetBucketCount(totalEventControlCount);
        var courses = courseBuilders
            .Select((x, i) => x.ToCourseMask(bucketCount, i))
            .ToImmutableArray();

        return new(totalEventControlCount, courses);
    }
}
