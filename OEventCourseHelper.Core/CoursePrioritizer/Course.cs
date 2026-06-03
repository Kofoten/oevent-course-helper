namespace OEventCourseHelper.Core.CoursePrioritizer;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
public readonly record struct Course(int CourseIndex, int CourseOffset, int ControlCount);
