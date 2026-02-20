using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record RequiredCoursesResult(BitMask Mask, ImmutableList<Course> Order);
