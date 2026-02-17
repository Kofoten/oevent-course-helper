using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class BitmaskBeamSearchSolverContext
{
    public const float MaximumRarity = 1.0F;

    public FrozenSet<CourseMask> CourseMasks { get; private init; }
    public ImmutableArray<float> ControlRarityLookup { get; private init; }
    public int TotalEventControlCount { get; private init; }
    public float TotalRaritySum { get; private init; }
    public int BucketCount { get; private init; }

    public BitmaskBeamSearchSolverContext(int totalEventControlCount, IEnumerable<CourseMask.Builder> courseMasksBuilders)
    {
        TotalEventControlCount = totalEventControlCount;
        BucketCount = ((totalEventControlCount - 1) >> 6) + 1;
        CourseMasks = courseMasksBuilders
            .Select(x => x.ToCourseMask(BucketCount))
            .ToFrozenSet();
        ControlRarityLookup = BuildControlRarityLookup(this);
        TotalRaritySum = ControlRarityLookup.Sum();
    }

    /// <summary>
    /// Builds an <see cref="ImmutableArray{float}"/> containing each controls rarity score mapped to it's global index value.
    /// </summary>
    /// <param name="courses">The set containing all courses.</param>
    /// <returns>A new instance of <see cref="ImmutableArray{float}"/>.</returns>
    private static ImmutableArray<float> BuildControlRarityLookup(BitmaskBeamSearchSolverContext context)
    {
        var controlFrequency = new int[context.TotalEventControlCount];
        var counter = new FrequencyCounter { Counts = controlFrequency };
        foreach (var course in context.CourseMasks)
        {
            course.ForEachControl(ref counter);
        }

        var rarityLookupBuilder = ImmutableArray.CreateBuilder<float>(context.TotalEventControlCount);
        for (int i = 0; i < context.TotalEventControlCount; i++)
        {
            rarityLookupBuilder.Add(controlFrequency[i] > 0 ? MaximumRarity / controlFrequency[i] : 0f);
        }

        return rarityLookupBuilder.DrainToImmutable();
    }
}
