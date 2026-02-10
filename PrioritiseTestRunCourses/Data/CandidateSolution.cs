using PrioritiseTestRunCourses.Extensions;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PrioritiseTestRunCourses.Data;

internal record CandidateSolution(
    ImmutableDictionary<string, int> Courses,
    ImmutableHashSet<string> UnvisitedControls,
    float RarityScore)
{
    public bool IsComplete => UnvisitedControls.Count == 0;

    /// <summary>
    /// Creates a new CandidateSolution initialized by the computed values in the provided controlsWithRarity dictionary.
    /// </summary>
    /// <param name="controlsWithRarity">The full set of controls with their pre calculated rarity (weight).</param>
    /// <returns>A new instance of CandidateSolution.</returns>
    public static CandidateSolution Initial(FrozenDictionary<string, float> controlsWithRarity) => new(
        ImmutableDictionary<string, int>.Empty,
        [.. controlsWithRarity.Keys],
        controlsWithRarity.Values.Sum());

    /// <summary>
    /// Computes a new CandidateSolution based on the added Course leaving the source CandidateSolution unmodified.
    /// </summary>
    /// <param name="course">The Course to add to the CandidateSolution</param>
    /// <returns>A new CandidateSolution instance containing the new solution state.</returns>
    public CandidateSolution AddCourse(
        Course course,
        FrozenDictionary<string, float> controlRarityLookup,
        float defaultRarity)
    {
        var newUnvisitedControls = UnvisitedControls.RemoveControls(
            course.Controls,
            controlRarityLookup,
            defaultRarity,
            out var rarityGain);

        return new CandidateSolution(
            Courses.Add(course.Name, Courses.Count),
            newUnvisitedControls,
            RarityScore - rarityGain);
    }

    internal class RarityPriorityComparer() : IComparer<CandidateSolution>
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

            return x.Courses.Count.CompareTo(y.Courses.Count);
        }
    }
}
