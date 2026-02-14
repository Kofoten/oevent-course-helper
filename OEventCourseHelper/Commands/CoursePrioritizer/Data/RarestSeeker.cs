using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Processor to find the rarest control in a <see cref="CourseMask"/>.
/// </summary>
/// <param name="rarityLookup">The lookup containing each controls rarity score.</param>
internal struct RarestSeeker(ImmutableArray<float> rarityLookup) : CourseMask.IProcessor
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
