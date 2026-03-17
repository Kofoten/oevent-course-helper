namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
internal record Course(int CourseIndex, string CourseName, BitMask ControlMask, int ControlCount)
{
    /// <summary>
    /// Builder for <see cref="Course"/>.
    /// </summary>
    internal class Builder(string courseName)
    {
        private readonly BitMask.Builder controlMaskBuilder = new();

        public string CourseName { get; private init; } = courseName;
        public int ControlCount { get; private set; } = 0;
        public uint CourseHash { get; private set; } = 0;

        public bool IsEmpty => controlMaskBuilder.IsZero;

        public void AddControl(int controlIndex, string controlCode)
        {
            if (controlMaskBuilder.Set(controlIndex))
            {
                ControlCount++;
                CourseHash ^= Crc32.Hash(controlCode);
            }
        }

        /// <summary>
        /// Builds the <see cref="Course"/> record.
        /// </summary>
        /// <param name="bucketCount">Total count of 64 bit buckets.</param>
        /// <returns>An instance of <see cref="Course"/>.</returns>
        public Course ToCourseMask(int bucketCount, int courseIndex)
        {
            return new Course(
                courseIndex,
                CourseName,
                controlMaskBuilder.ToBitMask(bucketCount),
                ControlCount);
        }
    }
}
