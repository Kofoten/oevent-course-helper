using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains a possible solution on which courses are required for the test run.
/// </summary>
internal record BitmaskCandidateSolution(
    ImmutableList<int> CourseOrder,
    ImmutableArray<ulong> IncludedCoursesMask,
    ImmutableArray<ulong> UnvisitedControlsMask,
    float RarityScore)
{
    /// <summary>
    /// Indicates if this solution covers all controls used in the orienteering event.
    /// </summary>
    public bool IsComplete => UnvisitedControlsMask.All(x => x == 0);

    /// <summary>
    /// Returns the current amount of courses required for the solution.
    /// </summary>
    public int CourseCount => CourseOrder.Count;

    /// <summary>
    /// Creates a new instance of <see cref="BitmaskCandidateSolution"/> with the bits for all controls in the entire
    /// orienteering event set to one and with the total rarity score of all theese controls summarized togheter.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>A new instance of <see cref="BitmaskCandidateSolution"/>.</returns>
    public static BitmaskCandidateSolution Initial(BitmaskBeamSearchSolverContext context)
    {
        var unvisitedControlsMask = ImmutableArray.CreateBuilder<ulong>(context.ControlMaskBucketCount);

        for (int i = 0; i < context.ControlMaskBucketCount - 1; i++)
        {
            unvisitedControlsMask.Add(ulong.MaxValue);
        }

        var remainder = context.TotalEventControlCount & 63;
        if (remainder == 0)
        {
            unvisitedControlsMask.Add(ulong.MaxValue);
        }
        else
        {
            unvisitedControlsMask.Add((1UL << remainder) - 1);
        }

        var includedCoursesMask = ImmutableCollectionsMarshal.AsImmutableArray(new ulong[context.CourseIdMaskBucketCount]);
        return new([], includedCoursesMask, unvisitedControlsMask.DrainToImmutable(), context.TotalControlRaritySum);
    }

    /// <summary>
    /// Computes a new instance of <see cref="BitmaskCandidateSolution"/> based on <paramref name="course"/> leaving
    /// the source <see cref="BitmaskCandidateSolution"/> unmodified.
    /// </summary>
    /// <param name="course">The <see cref="CourseMask"/> to add to the solution.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>A new instance of <see cref="BitmaskCandidateSolution"/> containing the modified state.</returns>
    public BitmaskCandidateSolution AddCourse(CourseMask course, BitmaskBeamSearchSolverContext context)
    {
        var newUnvisitedControlsMask = ImmutableArray.CreateBuilder<ulong>(UnvisitedControlsMask.Length);
        var rarityGain = 0.0F;

        for (int i = 0; i < UnvisitedControlsMask.Length; i++)
        {
            ulong unvisitedBucket = UnvisitedControlsMask[i];
            ulong courseBucket = course.ControlMask[i];

            rarityGain += GetBucketRarityGain(i, unvisitedBucket, courseBucket, context.ControlRarityLookup);

            newUnvisitedControlsMask.Add(unvisitedBucket & ~courseBucket);
        }

        var newIncludedCoursesMask = new ulong[context.CourseIdMaskBucketCount];
        for (int i = 0; i < context.CourseIdMaskBucketCount; i++)
        {
            if (i == course.CourseId.BucketIndex)
            {
                newIncludedCoursesMask[i] = IncludedCoursesMask[i] | course.CourseId.BucketMask;
            }
            else
            {
                newIncludedCoursesMask[i] = IncludedCoursesMask[i];
            }
        }

        return new BitmaskCandidateSolution(
            CourseOrder.Add(course.CourseId.Index),
            ImmutableCollectionsMarshal.AsImmutableArray(newIncludedCoursesMask),
            newUnvisitedControlsMask.DrainToImmutable(),
            RarityScore - rarityGain);
    }

    /// <summary>
    /// Calculates the rarity that would be gained by adding <paramref name="course"/> to this solution.
    /// </summary>
    /// <param name="course">The <see cref="CourseMask"/> to calculate rarity gain for.</param>
    /// <param name="controlRarityLookup">The lookup containing each controls rarity score.</param>
    /// <returns>The calculated gain to this solution by including the provided <see cref="CourseMask"/>.</returns>
    public float GetPotentialRarityGain(CourseMask course, ImmutableArray<float> controlRarityLookup)
    {
        var rarityGain = 0.0F;
        for (int i = 0; i < UnvisitedControlsMask.Length; i++)
        {
            ulong unvisitedBucket = UnvisitedControlsMask[i];
            ulong courseBucket = course.ControlMask[i];

            rarityGain += GetBucketRarityGain(i, unvisitedBucket, courseBucket, controlRarityLookup);
        }

        return rarityGain;
    }

    /// <summary>
    /// Checks if the solution already contains the specified <see cref="CourseMask"/>.
    /// </summary>
    /// <param name="courseMask">The <see cref="CourseMask"/> to check.</param>
    /// <returns>True if the solution already contains the <see cref="CourseMask"/>; otherwise False.</returns>
    public bool ContainsCourseMask(CourseMask courseMask)
    {
        var result = IncludedCoursesMask[courseMask.CourseId.BucketIndex] & courseMask.CourseId.BucketMask;
        return result != 0;
    }

    /// <summary>
    /// Internal method for calculating the rarity gain between two buckets in order for <see cref="AddCourse"/>
    /// and <see cref="GetPotentialRarityGain"/> to compute the exact same value.
    /// </summary>
    private static float GetBucketRarityGain(
        int bucketIndex,
        ulong unvisitedBucket,
        ulong courseBucket,
        ImmutableArray<float> controlRarityLookup)
    {
        var rarityGain = 0.0F;

        ulong visits = unvisitedBucket & courseBucket;
        while (visits != 0)
        {
            int bit = BitOperations.TrailingZeroCount(visits);
            int globalControlIndex = (bucketIndex << 6) | bit;

            if (globalControlIndex < controlRarityLookup.Length)
            {
                rarityGain += controlRarityLookup[globalControlIndex];
            }

            visits &= ~(1UL << bit);
        }

        return rarityGain;
    }

    /// <summary>
    /// A comparere for <see cref="BitmaskCandidateSolution"/> that prioritizes control rarity over control count.
    /// </summary>
    internal class RarityComparer() : IComparer<BitmaskCandidateSolution>
    {
        public int Compare(BitmaskCandidateSolution? x, BitmaskCandidateSolution? y)
        {
            if (x is null)
            {
                return y is null ? 0 : -1;
            }

            if (y is null)
            {
                return 1;
            }

            var rarityComparison = x.RarityScore.CompareTo(y.RarityScore);
            if (rarityComparison != 0)
            {
                return rarityComparison;
            }

            return x.CourseCount.CompareTo(y.CourseCount);
        }
    }
}
