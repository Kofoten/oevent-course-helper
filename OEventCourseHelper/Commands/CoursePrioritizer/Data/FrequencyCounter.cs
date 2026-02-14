namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Processor to add 1 to an index in an integer array matching the controls index.
/// </summary>
struct FrequencyCounter : CourseMask.IProcessor
{
    public int[] Counts;

    public readonly void Process(int index) => Counts[index]++;
}
