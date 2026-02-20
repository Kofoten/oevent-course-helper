namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
internal record CourseMask(int CourseIndex, string CourseName, BitMask ControlMask, int ControlCount)
{
    /// <summary>
    /// Builder for <see cref="CourseMask"/>.
    /// </summary>
    internal class Builder()
    {
        public string CourseName { get; set; } = "Unknown Course";
        public IList<ulong> ControlMask { get; set; } = [];
        public int ControlCount { get; set; } = 0;

        /// <summary>
        /// Builds the <see cref="CourseMask"/> record.
        /// </summary>
        /// <param name="bucketCount">Total count of 64 bit buckets.</param>
        /// <returns>An instance of <see cref="CourseMask"/>.</returns>
        public CourseMask ToCourseMask(int bucketCount, int courseIndex)
        {
            var mask = new ulong[bucketCount];
            for (int i = 0; i < ControlMask.Count; i++)
            {
                mask[i] = ControlMask[i];
            }

            return new CourseMask(
                courseIndex,
                CourseName,
                BitMask.Create(mask),
                ControlCount);
        }
    }

    /// <summary>
    /// Interface for use with <see cref="ForEachControl"/>.
    /// </summary>
    internal interface IProcessor
    {
        void Process(int controlIndex, CourseMask courseMask);
    }
}
