using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.CoursePrioritizer;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
public record Course(int CourseIndex, string CourseName, BitMask ControlMask, int ControlCount);
