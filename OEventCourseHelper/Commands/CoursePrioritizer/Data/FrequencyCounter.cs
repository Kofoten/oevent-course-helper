namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Processor to add 1 to an index in an integer array matching the controls index.
/// </summary>
/// <param name="Counts">The array of counters for each control.</param>
internal struct FrequencyCounter(int[] Counts) : CourseMask.IProcessor
{
    public readonly void Process(int index, CourseMask _) => Counts[index]++;
}
