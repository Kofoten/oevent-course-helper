using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class BitmaskBeamSearchSolverContext(
    int totalEventControlCount,
    float totalRaritySum,
    int bucketCount,
    FrozenSet<CourseMask> courseMasks,
    ImmutableArray<float> controlRarityLookup)
{
    public const float MaximumRarity = 1.0F;

    public FrozenSet<CourseMask> CourseMasks { get; private init; } = courseMasks;
    public ImmutableArray<float> ControlRarityLookup { get; private init; } = controlRarityLookup;
    public int TotalEventControlCount { get; private init; } = totalEventControlCount;
    public float TotalRaritySum { get; private init; } = totalRaritySum;
    public int BucketCount { get; private init; } = bucketCount;

    /// <summary>
    /// Builds a new <see cref="BitmaskBeamSearchSolverContext"/> from <paramref name="courseMasksBuilders"/>.
    /// </summary>
    /// <param name="totalEventControlCount">The total number of controls in the event.</param>
    /// <param name="courseMasksBuilders">The course mask builders to create the context from.</param>
    /// <returns>A new instance of <see cref="BitmaskBeamSearchSolverContext"/>.</returns>
    public static BitmaskBeamSearchSolverContext Create(int totalEventControlCount, IEnumerable<CourseMask.Builder> courseMasksBuilders)
    {
        var bucketCount = ((totalEventControlCount - 1) >> 6) + 1;
        var courseMasks = courseMasksBuilders
            .Select(x => x.ToCourseMask(bucketCount))
            .ToFrozenSet();

        var controlRarityLookup = BuildControlRarityLookup(totalEventControlCount, courseMasks);
        var totalRaritySum = controlRarityLookup.Sum();

        return new(
            totalEventControlCount,
            totalRaritySum,
            bucketCount,
            courseMasks,
            controlRarityLookup);
    }

    /// <summary>
    /// Builds an <see cref="ImmutableArray{float}"/> containing each controls rarity score mapped to it's global index value.
    /// </summary>
    /// <param name="courses">The set containing all courses.</param>
    /// <returns>A new instance of <see cref="ImmutableArray{float}"/>.</returns>
    private static ImmutableArray<float> BuildControlRarityLookup(int totalEventControlCount, FrozenSet<CourseMask> courseMasks)
    {
        var controlFrequency = new int[totalEventControlCount];
        var counter = new FrequencyCounter { Counts = controlFrequency };
        foreach (var course in courseMasks)
        {
            course.ForEachControl(ref counter);
        }

        var rarityLookupBuilder = ImmutableArray.CreateBuilder<float>(totalEventControlCount);
        for (int i = 0; i < totalEventControlCount; i++)
        {
            rarityLookupBuilder.Add(controlFrequency[i] > 0 ? MaximumRarity / controlFrequency[i] : 0f);
        }

        return rarityLookupBuilder.DrainToImmutable();
    }
}
