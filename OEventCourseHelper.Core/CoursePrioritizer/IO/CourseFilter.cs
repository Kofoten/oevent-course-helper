using System.Collections.Immutable;

namespace OEventCourseHelper.Core.CoursePrioritizer.IO;

public class CourseFilter(bool FilterEmpty, ImmutableArray<string> NameIncludes)
{
    /// <summary>
    /// Checks if <paramref name="course"/> matches the filter.
    /// </summary>
    /// <param name="builder">The <see cref="Course"/> to check.</param>
    /// <returns>True if the <see cref="Course"/> matches the filter; otherwise False.</returns>
    public bool Matches(Course course)
    {
        if (FilterEmpty && course.ControlMask.IsZero)
        {
            return false;
        }

        if (NameIncludes.Length > 0 && !NameIncludes.Any(course.CourseName.Contains))
        {
            return false;
        }

        return true;
    }
}
