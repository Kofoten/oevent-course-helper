using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

internal class CourseBuilderFilter(bool FilterEmpty, ImmutableArray<string> NameIncludes)
{
    /// <summary>
    /// Checks if <paramref name="builder"/> matches the filter.
    /// </summary>
    /// <param name="builder">The <see cref="Course.Builder"/> to check.</param>
    /// <returns>True if the <see cref="Course.Builder"/> matches the filter; otherwise False.</returns>
    public bool Matches(Course.Builder builder)
    {
        if (FilterEmpty && builder.ControlMaskBuilder.IsZero)
        {
            return false;
        }

        if (NameIncludes.Length > 0 && !NameIncludes.Any(builder.CourseName.Contains))
        {
            return false;
        }

        return true;
    }
}
