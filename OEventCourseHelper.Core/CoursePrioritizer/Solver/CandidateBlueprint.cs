using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.CoursePrioritizer.Solver;

/// <summary>
/// A blueprint used to materialize an instance of <see cref="CandidateSolution"/>.
/// </summary>
/// <remarks>
/// Creates a new blueprint from a <see cref="CandidateSolution"/>, a <see cref="Course"/> and the projected rarity score.
/// </remarks>
/// <param name="parent">The parent solution for the blueprint.</param>
/// <param name="addedCourse">The course to be included in the solution when materialized.</param>
/// <param name="projectedRarityScore">The projected rarity score when the course get added.</param>
internal readonly struct CandidateBlueprint(CandidateSolution parent, Course addedCourse, ulong projectedRarityScore)
{
    private readonly CandidateSolution parent = parent;
    private readonly Course addedCourse = addedCourse;

    /// <summary>
    /// The number of courses that would be included in the materialized solution.
    /// </summary>
    public int CourseCount { get; private init; } = parent.CourseCount + 1;

    /// <summary>
    /// The rarity score of the solution that would be materialized.
    /// </summary>
    public ulong RarityScore { get; private init; } = projectedRarityScore;

    /// <summary>
    /// Materializes a <see cref="CandidateSolution"/> based on this blueprint.
    /// </summary>
    /// <returns>A new instance of <see cref="CandidateSolution"/> with the course from the blueprint included.</returns>
    public CandidateSolution Materialize(BitMask courseMask, int controlMaskBucketCount)
    {
        var courseSlice = courseMask.Slice(addedCourse.CourseOffset, controlMaskBucketCount);
        return new CandidateSolution(
            parent.CourseOrder.Add(addedCourse),
            parent.IncludedCoursesMask.Set(addedCourse.CourseIndex),
            parent.UnvisitedControlsMask.AndNot(courseSlice),
            RarityScore);
    }

    /// <summary>
    /// Comparer that compares blueprints based on their rarity scores. If two blueprints
    /// have the exact same rarity score and the same projected included courses mask they
    /// are considered equal (they are permutations of each other).
    /// </summary>
    public readonly struct RarityComparer : IComparer<CandidateBlueprint>
    {
        public int Compare(CandidateBlueprint x, CandidateBlueprint y)
        {
            var rarityResult = x.RarityScore.CompareTo(y.RarityScore);
            if (rarityResult != 0)
            {
                return rarityResult;
            }

            var xBucketMask = BitMask.BucketMask.FromBitIndex(x.addedCourse.CourseIndex);
            var yBucketMask = BitMask.BucketMask.FromBitIndex(y.addedCourse.CourseIndex);

            for (int i = 0; i < x.parent.IncludedCoursesMask.BucketCount; i++)
            {
                ulong xBucket = x.parent.IncludedCoursesMask.Buckets[i];
                if (i == xBucketMask.BucketIndex)
                {
                    xBucket |= xBucketMask.BucketValue;
                }

                ulong yBucket = y.parent.IncludedCoursesMask.Buckets[i];
                if (i == yBucketMask.BucketIndex)
                {
                    yBucket |= yBucketMask.BucketValue;
                }

                if (xBucket != yBucket)
                {
                    return xBucket.CompareTo(yBucket);
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// Used to resolve a tie when using the <see cref="RarityComparer"/>. This will prefer
    /// the blueprint where the parent solution has the best rarity score (least remaining work).
    /// If they are still equal the index of the first course added is used to resolve the tie.
    /// </summary>
    public readonly struct TieBreakComparer : IComparer<CandidateBlueprint>
    {
        public int Compare(CandidateBlueprint x, CandidateBlueprint y)
        {
            var rarityResult = x.parent.RarityScore.CompareTo(y.parent.RarityScore);
            if (rarityResult != 0)
            {
                return rarityResult;
            }

            if (x.parent.CourseOrder.IsEmpty)
            {
                return y.parent.CourseOrder.IsEmpty ? 0 : -1;
            }
            else if (y.parent.CourseOrder.IsEmpty)
            {
                return 1;
            }

            return x.parent.CourseOrder[0].CourseIndex.CompareTo(y.parent.CourseOrder[0].CourseIndex);
        }
    }
}
