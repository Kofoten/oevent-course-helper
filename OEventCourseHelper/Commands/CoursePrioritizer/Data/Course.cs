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
        public IList<ulong> ControlMask { get; set; } = [];
        public int ControlCount { get; set; } = 0;

        /// <summary>
        /// Builds the <see cref="Course"/> record.
        /// </summary>
        /// <param name="bucketCount">Total count of 64 bit buckets.</param>
        /// <returns>An instance of <see cref="Course"/>.</returns>
        public Course ToCourseMask(int bucketCount, int courseIndex)
        {
            var mask = new ulong[bucketCount];
            for (int i = 0; i < ControlMask.Count; i++)
            {
                mask[i] = ControlMask[i];
            }

            return new Course(
                courseIndex,
                CourseName,
                BitMask.Create(mask),
                ControlCount);
        }
    }
}
