using OEventCourseHelper.Extensions;
using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record BitmaskCandidateSolution(
    ImmutableList<string> Courses,
    ImmutableArray<ulong> UnvisitedControlsMask,
    float RarityScore)
{
    public bool IsComplete => UnvisitedControlsMask.All(x => x == 0);

    /// <summary>
    /// Creates a new CandidateSolution initialized by the computed values in the provided controlsWithRarity dictionary.
    /// </summary>
    /// <param name="controlsWithRarity">The full set of controls with their pre calculated rarity (weight).</param>
    /// <returns>A new instance of CandidateSolution.</returns>
    public static BitmaskCandidateSolution Initial(int totalEventControlCount, ImmutableArray<float> controlRarityLookup)
    {
        var bucketCount = totalEventControlCount.GetUnsignedLongBucketCount();
        var unvisitedControlsMask = ImmutableArray.CreateBuilder<ulong>(bucketCount);

        for (int i = 0; i < bucketCount - 1; i++)
        {
            unvisitedControlsMask[i] = ulong.MaxValue;
        }

        var remainder = totalEventControlCount & 63;
        if (remainder == 0)
        {
            unvisitedControlsMask[^1] = ulong.MaxValue;
        }
        else
        {
            unvisitedControlsMask[^1] = (1UL << remainder) - 1;
        }

        return new([], unvisitedControlsMask.DrainToImmutable(), controlRarityLookup.Sum());
    }

    /// <summary>
    /// Computes a new CandidateSolution based on the added Course leaving the source CandidateSolution unmodified.
    /// </summary>
    /// <param name="course">The Course to add to the CandidateSolution</param>
    /// <returns>A new CandidateSolution instance containing the new solution state.</returns>
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
