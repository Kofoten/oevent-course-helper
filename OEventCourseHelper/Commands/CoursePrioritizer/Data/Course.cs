namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
internal record Course(int CourseIndex, string CourseName, BitMask ControlMask, int ControlCount);
