using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Processor to find the rarest control in a <see cref="CourseMask"/>.
/// </summary>
/// <param name="RarityLookup">The lookup containing each controls rarity score.</param>
internal struct RarestSeeker(ImmutableArray<float> RarityLookup) : CourseMask.IProcessor
{
    private float maxRarity = -1f;
    public int IndexOfRarest = -1;

    public void Process(int index, CourseMask _)
    {
        if (index < RarityLookup.Length && maxRarity < RarityLookup[index])
        {
            maxRarity = RarityLookup[index];
            IndexOfRarest = index;
        }
    }
}
