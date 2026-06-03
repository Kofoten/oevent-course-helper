using System.Collections.Immutable;

namespace OEventCourseHelper.Core.CoursePrioritizer.IO;

internal record CourseFilter(bool FilterEmpty, ImmutableArray<string> NameIncludes);
