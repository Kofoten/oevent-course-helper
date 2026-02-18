using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class BitmaskBeamSearchSolverContext(
    int totalEventControlCount,
    float totalControlRaritySum,
    int controlMaskBucketCount,
    int courseIdMaskBucketCount,
    ImmutableArray<CourseMask> courseMasks,
    ImmutableArray<float> controlRarityLookup,
    ImmutableArray<ImmutableArray<ulong>> courseIdInvertedIndex)
{
    public const float MaximumRarity = 1.0F;

    public ImmutableArray<CourseMask> CourseMasks { get; private init; } = courseMasks;
    public ImmutableArray<float> ControlRarityLookup { get; private init; } = controlRarityLookup;
    public ImmutableArray<ImmutableArray<ulong>> CourseInvertedIndex { get; private set; } = courseIdInvertedIndex;
    public int TotalEventControlCount { get; private init; } = totalEventControlCount;
    public float TotalControlRaritySum { get; private init; } = totalControlRaritySum;
    public int ControlMaskBucketCount { get; private init; } = controlMaskBucketCount;
    public int CourseIdMaskBucketCount { get; private init; } = courseIdMaskBucketCount;

    /// <summary>
    /// Builds a new <see cref="BitmaskBeamSearchSolverContext"/> from <paramref name="courseMasksBuilders"/>.
    /// </summary>
    /// <param name="totalEventControlCount">The total number of controls in the event.</param>
    /// <param name="courseMasksBuilders">The course mask builders to create the context from.</param>
    /// <returns>A new instance of <see cref="BitmaskBeamSearchSolverContext"/>.</returns>
    public static BitmaskBeamSearchSolverContext Create(int totalEventControlCount, IEnumerable<CourseMask.Builder> courseMasksBuilders)
    {
        var courseCount = courseMasksBuilders.Count();
        var controlMaskBucketCount = ((totalEventControlCount - 1) >> 6) + 1;
        var courseIdMaskBucketCount = ((courseCount - 1) >> 6) + 1;
        var courseIdInvertedIndexCache = new ulong[totalEventControlCount][];
        var invertedIndexProcessor = new InvertedIndexProcessor(courseIdInvertedIndexCache, courseIdMaskBucketCount);
        var courseMasks = courseMasksBuilders
            .Select((x, i) =>
            {
                var courseMask = x.ToCourseMask(controlMaskBucketCount, i);
                courseMask.ForEachControl(ref invertedIndexProcessor);
                return courseMask;
            })
            .ToImmutableArray();

        var controlRarityLookup = BuildControlRarityLookup(totalEventControlCount, courseMasks);
        var totalControlRaritySum = controlRarityLookup.Sum();
        var courseIdInvertedIndex = new ImmutableArray<ulong>[totalEventControlCount];

        for (int i = 0; i < totalEventControlCount; i++)
        {
            courseIdInvertedIndex[i] = ImmutableCollectionsMarshal.AsImmutableArray(courseIdInvertedIndexCache[i]);
        }

        return new(
            totalEventControlCount,
            totalControlRaritySum,
            controlMaskBucketCount,
            courseIdMaskBucketCount,
            courseMasks,
            controlRarityLookup,
            ImmutableCollectionsMarshal.AsImmutableArray(courseIdInvertedIndex));
    }

    /// <summary>
    /// Builds an <see cref="ImmutableArray{float}"/> containing each controls rarity score mapped to it's global index value.
    /// </summary>
    /// <param name="courseMasks">The set containing all courses.</param>
    /// <returns>A new instance of <see cref="ImmutableArray{float}"/>.</returns>
    private static ImmutableArray<float> BuildControlRarityLookup(int totalEventControlCount, IEnumerable<CourseMask> courseMasks)
    {
        var controlFrequency = new int[totalEventControlCount];
        var counter = new FrequencyCounter(controlFrequency);
        foreach (var course in courseMasks)
        {
            course.ForEachControl(ref counter);
        }

        var rarityLookup = new float[totalEventControlCount];
        for (int i = 0; i < totalEventControlCount; i++)
        {
            rarityLookup[i] = (controlFrequency[i] > 0 ? MaximumRarity / controlFrequency[i] : 0.0F);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(rarityLookup);
    }
}
