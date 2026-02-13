using OEventCourseHelper.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal struct RarestSeeker(ImmutableArray<float> rarityLookup) : IProcessor
{
    private float maxRarity = -1f;
    public int IndexOfRarest = -1;

    public void Process(int index)
    {
        if (index < rarityLookup.Length && maxRarity < rarityLookup[index])
        {
            maxRarity = rarityLookup[index];
            IndexOfRarest = index;
        }
    }
}
