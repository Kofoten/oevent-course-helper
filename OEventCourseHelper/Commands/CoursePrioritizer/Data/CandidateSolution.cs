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
        var includedCoursesMask = BitMask.Zero(context.CourseMaskBucketCount);
        return new([], includedCoursesMask, unvisitedControlMask, context.TotalControlRaritySum);
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
}
