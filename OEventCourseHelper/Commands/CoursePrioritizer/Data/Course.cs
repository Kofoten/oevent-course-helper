namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
internal record Course(int CourseIndex, string CourseName, BitMask ControlMask, int ControlCount)
{
    /// <summary>
    /// Builder for <see cref="Course"/>.
    /// </summary>
    internal class Builder()
    {
        public string CourseName { get; set; } = "Unknown Course";
        public BitMask.Builder ControlMaskBuilder { get; set; } = new();
        public int ControlCount { get; set; } = 0;

        public Builder(string courseName)
            : this()
        {
            this.CourseName = courseName;
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
                ControlMaskBuilder.ToBitMask(bucketCount),
                ControlCount);
        }
    }
}
