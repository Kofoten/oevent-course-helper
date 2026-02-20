using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains a possible solution on which courses are required for the test run.
/// </summary>
internal record CandidateSolution(
    ImmutableList<Course> CourseOrder,
    BitMask IncludedCoursesMask,
    BitMask UnvisitedControlsMask,
    float RarityScore)
{
    /// <summary>
    /// Indicates if this solution covers all controls used in the orienteering event.
    /// </summary>
    public bool IsComplete => UnvisitedControlsMask.IsZero;

    /// <summary>
    /// Returns the current amount of courses required for the solution.
    /// </summary>
    public int CourseCount => CourseOrder.Count;

    /// <summary>
    /// Creates a new instance of <see cref="CandidateSolution"/> with the bits for all controls in the entire
    /// orienteering event set to one and with the total rarity score of all theese controls summarized togheter.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>A new instance of <see cref="CandidateSolution"/>.</returns>
    public static CandidateSolution Initial(BeamSearchSolverContext context)
    {
        var unvisitedControlMask = BitMask.Fill(context.TotalEventControlCount);
        var includedCoursesMask = BitMask.Create(new ulong[context.CourseMaskBucketCount]);
        return new([], includedCoursesMask, unvisitedControlMask, context.TotalControlRaritySum);
    }

    /// <summary>
    /// Computes a new instance of <see cref="CandidateSolution"/> based on <paramref name="course"/> leaving
    /// the source <see cref="CandidateSolution"/> unmodified.
    /// </summary>
    /// <param name="course">The <see cref="Course"/> to add to the solution.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>A new instance of <see cref="CandidateSolution"/> containing the modified state.</returns>
    public CandidateSolution AddCourse(Course course, BeamSearchSolverContext context)
    {
        var newUnvisitedControlsMask = new ulong[context.ControlMaskBucketCount];
        var rarityGain = 0.0F;

        for (int i = 0; i < context.ControlMaskBucketCount; i++)
        {
            var overlap = UnvisitedControlsMask.Buckets[i] & course.ControlMask.Buckets[i];
            var bucketEnumerator = new BitMask.BucketEnumerator(i, overlap);
            while (bucketEnumerator.MoveNext())
            {
                rarityGain += context.ControlRarityLookup[bucketEnumerator.Current];
            }

            newUnvisitedControlsMask[i] = UnvisitedControlsMask.Buckets[i] & ~course.ControlMask.Buckets[i];
        }

        return new CandidateSolution(
            CourseOrder.Add(course),
            IncludedCoursesMask.Set(course.CourseIndex),
            BitMask.Create(newUnvisitedControlsMask),
            RarityScore - rarityGain);
    }

    /// <summary>
    /// Calculates the rarity that would be gained by adding <paramref name="course"/> to this solution.
    /// </summary>
    /// <param name="course">The <see cref="Course"/> to calculate rarity gain for.</param>
    /// <param name="controlRarityLookup">The lookup containing each controls rarity score.</param>
    /// <returns>The calculated gain to this solution by including the provided <see cref="Course"/>.</returns>
    public float GetPotentialRarityGain(Course course, ImmutableArray<float> controlRarityLookup)
    {
        var rarityGain = 0.0F;
        foreach (var controlIndex in course.ControlMask)
        {
            if (UnvisitedControlsMask[controlIndex])
            {
                rarityGain += controlRarityLookup[controlIndex];
            }
        }

        return rarityGain;
    }

    /// <summary>
    /// A comparere for <see cref="CandidateSolution"/> that prioritizes control rarity over control count.
    /// </summary>
    internal class RarityComparer() : IComparer<CandidateSolution>
    {
        public int Compare(CandidateSolution? x, CandidateSolution? y)
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
