using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

internal class CourseMaskFilter(bool IgnoreEmpty, ImmutableArray<string> NameIncludes)
{
    /// <summary>
    /// Filters <paramref name="courses"/> based of the settings set in the fileter.
    /// </summary>
    /// <param name="courses">The list of <see cref="CourseMask"/> items to filer.</param>
    /// <returns>An <see cref="IEnumerable{CourseMask}"/> containing the items that passed teh filter.</returns>
    public IEnumerable<CourseMask> Filter(IEnumerable<CourseMask> courses)
    {
        foreach (var course in courses)
        {
            if (IgnoreEmpty && course.ControlMask.All(x => x == 0))
            {
                continue;
            }

            if (NameIncludes.Length > 0 && !NameIncludes.Any(course.CourseName.Contains))
            {
                continue;
            }

            yield return course;
        }
    }
}
