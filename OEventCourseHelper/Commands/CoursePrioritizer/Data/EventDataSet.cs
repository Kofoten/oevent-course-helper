using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// A data set containing event data.
/// </summary>
/// <param name="Controls">The controls in the event.</param>
/// <param name="Courses">The courses in the event.</param>
internal record EventDataSet(ImmutableArray<string> Controls, ImmutableArray<Course> Courses);
