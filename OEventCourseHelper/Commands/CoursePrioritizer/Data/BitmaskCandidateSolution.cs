using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains a possible solution on which courses are required for the test run.
/// </summary>
internal record BitmaskCandidateSolution(
    ImmutableList<CourseMask> CourseOrder,
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
    /// Creates a new instance of <see cref="BitmaskCandidateSolution"/> with the bits for all controls in the entire
    /// orienteering event set to one and with the total rarity score of all theese controls summarized togheter.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>A new instance of <see cref="BitmaskCandidateSolution"/>.</returns>
    public static BitmaskCandidateSolution Initial(BitmaskBeamSearchSolverContext context)
    {
        var unvisitedControlMask = BitMask.Fill(context.TotalEventControlCount);
        var includedCoursesMask = BitMask.Create(new ulong[context.CourseIdMaskBucketCount]);
        return new([], includedCoursesMask, unvisitedControlMask, context.TotalControlRaritySum);
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
        var newUnvisitedControlsMask = UnvisitedControlsMask.AndNot(course.ControlMask);
        var rarityGain = GetPotentialRarityGain(course, context.ControlRarityLookup);

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
            CourseOrder.Add(course),
            ImmutableCollectionsMarshal.AsImmutableArray(newIncludedCoursesMask),
            newUnvisitedControlsMask,
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
