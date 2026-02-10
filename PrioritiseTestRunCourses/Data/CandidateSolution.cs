using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PrioritiseTestRunCourses.Data;

internal record CandidateSolution(
    ImmutableDictionary<string, int> Courses,
    ImmutableHashSet<string> UnvisitedControls,
    float RarityScore)
{
    public bool IsComplete => UnvisitedControls.Count == 0;

    public static CandidateSolution Initial(FrozenDictionary<string, float> controlsWithRarity) => new(
        ImmutableDictionary<string, int>.Empty,
        [.. controlsWithRarity.Keys],
        controlsWithRarity.Values.Sum());

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
