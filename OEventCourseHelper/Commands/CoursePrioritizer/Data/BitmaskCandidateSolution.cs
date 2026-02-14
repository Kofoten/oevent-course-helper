using OEventCourseHelper.Extensions;
using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains a possible solution on which courses are required for the test run.
/// </summary>
internal record BitmaskCandidateSolution(
    ImmutableList<string> Courses,
    ImmutableArray<ulong> UnvisitedControlsMask,
    float RarityScore)
{
    /// <summary>
    /// Indicates if this solution covers all controls used in the orienteering event.
    /// </summary>
    public bool IsComplete => UnvisitedControlsMask.All(x => x == 0);

    /// <summary>
    /// Creates a new instance of <see cref="BitmaskCandidateSolution"/> with the bits for all controls in the entire
    /// orienteering event set to one and with the total rarity score of all theese controls summarized togheter.
    /// </summary>
    /// <param name="totalEventControlCount">The total number of controls used by the orienteering event.</param>
    /// <param name="controlRarityLookup">The lookup containing each controls rarity score.</param>
    /// <returns>A new instance of <see cref="BitmaskCandidateSolution"/>.</returns>
    public static BitmaskCandidateSolution Initial(int totalEventControlCount, ImmutableArray<float> controlRarityLookup)
    {
        var bucketCount = totalEventControlCount.Get64BitBucketCount();
        var unvisitedControlsMask = ImmutableArray.CreateBuilder<ulong>(bucketCount);

        for (int i = 0; i < bucketCount - 1; i++)
        {
            unvisitedControlsMask.Add(ulong.MaxValue);
        }

        var remainder = totalEventControlCount & 63;
        if (remainder == 0)
        {
            unvisitedControlsMask.Add(ulong.MaxValue);
        }
        else
        {
            unvisitedControlsMask.Add((1UL << remainder) - 1);
        }

        return new([], unvisitedControlsMask.DrainToImmutable(), controlRarityLookup.Sum());
    }

    /// <summary>
    /// Computes a new instance of <see cref="BitmaskCandidateSolution"/> based on <paramref name="course"> leaving
    /// the source <see cref="BitmaskCandidateSolution"/> unmodified.
    /// </summary>
    /// <param name="course">The <see cref="CourseMask"/> to add to the solution.</param>
    /// <param name="controlRarityLookup">The lookup containing each controls rarity score.</param>
    /// <returns>A new instance of <see cref="BitmaskCandidateSolution"/> containing the modified state.</returns>
    public BitmaskCandidateSolution AddCourse(CourseMask course, ImmutableArray<float> controlRarityLookup)
    {
        var newUnvisitedControlsMask = ImmutableArray.CreateBuilder<ulong>(UnvisitedControlsMask.Length);
        var rarityGain = 0.0F;

        for (int i = 0; i < UnvisitedControlsMask.Length; i++)
        {
            ulong unvisitedBucket = UnvisitedControlsMask[i];
            ulong courseBucket = course.ControlMask[i];

            rarityGain += GetBucketRarityGain(i, unvisitedBucket, courseBucket, controlRarityLookup);

            newUnvisitedControlsMask.Add(unvisitedBucket & ~courseBucket);
        }

        return new BitmaskCandidateSolution(
            Courses.Add(course.CourseName),
            newUnvisitedControlsMask.MoveToImmutable(),
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

            return x.Courses.Count.CompareTo(y.Courses.Count);
        }
    }
}
