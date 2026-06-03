using OEventCourseHelper.Core.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Core.CoursePrioritizer.IO;

/// <summary>
/// A data set containing event data.
/// </summary>
/// <param name="Controls">The controls in the event.</param>
/// <param name="Courses">The courses in the event.</param>
internal record EventDataSet(
    ImmutableArray<string> Controls,
    ImmutableArray<string> CourseNames,
    ImmutableArray<Course> Courses,
    BitMask CourseMask,
    int ControlMaskBucketCount);
