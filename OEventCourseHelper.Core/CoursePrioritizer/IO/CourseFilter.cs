using System.Collections.Immutable;

namespace OEventCourseHelper.Core.CoursePrioritizer.IO;

internal record CourseFilter(bool FilterEmpty, ImmutableArray<string> NameIncludes)
{
    public bool Matches(string courseName, int controlCount)
    {
        if (FilterEmpty && controlCount == 0)
        {
            return false;
        }

        if (NameIncludes.Length > 0 && !NameIncludes.Any(courseName.Contains))
        {
            return false;
        }

        return true;
    }
}
