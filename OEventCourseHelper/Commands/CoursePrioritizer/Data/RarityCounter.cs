using OEventCourseHelper.Data;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

struct RarityCounter : IProcessor
{
    public int[] Counts;

    public readonly void Process(int index) => Counts[index]++;
}
