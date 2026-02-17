using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

internal class CourseMaskBuilderFilter(bool FilterEmpty, ImmutableArray<string> NameIncludes)
{
    /// <summary>
    /// Checks if <paramref name="builder"/> matches the filter.
    /// </summary>
    /// <param name="builder">The <see cref="CourseMask.Builder"/> to check.</param>
    /// <returns>True if the <see cref="CourseMask.Builder"/> matches the filter; otherwise False.</returns>
    public bool Matches(CourseMask.Builder builder)
    {
        if (FilterEmpty && builder.ControlMask.All(x => x == 0))
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
