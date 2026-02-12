using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Commands.CoursePrioritizer;

internal class BeamSearchSolver(int BeamWidth)
{
    private const float MaximumRarity = 1.0F;

    private static readonly CandidateSolution.RarityPriorityComparer candidateSolutionComparer = new();

    public bool TrySolve(IEnumerable<Course> courses, [NotNullWhen(true)] out CourseResult[]? result)
    {
        // Create an inverted index for the courses by using the controls as keys.
        var coursesInvertedIndex = courses
            .SelectMany(x => x.Controls, (x, y) => (ControlCode: y, Course: x))
            .ToLookup(x => x.ControlCode, x => x.Course);

        // Create the control rarity lookup.
        var controlRarityLookup = coursesInvertedIndex
            .ToFrozenDictionary(x => x.Key, x => MaximumRarity / x.Count());

        // Calculate dominated courses.
        var dominatedCourses = courses
            .Where(x => IsDominated(x, coursesInvertedIndex, controlRarityLookup))
            .Select(x => x.Name)
            .ToFrozenSet();

        // Create a set of the courses that should be evaluated by the beam search.
        var availableCourses = courses
            .Where(x => !dominatedCourses.Contains(x.Name))
            .ToFrozenSet();

        // Perform the beam search to get the required courses ordered by rarity.
        var requiredCourses = FindRequiredCourseOrder(availableCourses, controlRarityLookup);
        if (requiredCourses is null)
        {
            result = null;
            return false;
        }

        // Combine the lists/sets into the final result.
        var requiredCoursesSet = requiredCourses.ToFrozenSet();
        result = [
            .. requiredCourses.Select(x => new CourseResult(x, true)),
            .. availableCourses
                .Where(x => !requiredCoursesSet.Contains(x.Name))
                .OrderByDescending(x => x.Controls.Count)
                .ThenBy(x => x.Name)
                .Select(x => new CourseResult(x.Name, false)),
            .. dominatedCourses.Order().Select(x => new CourseResult(x, false)),
        ];

        return true;
    }

    /// <summary>
    /// Computes the smallest amount of required courses and returns them in a prioritized order
    /// based on the rarity of the courses controls using a beam search algorithm.
    /// </summary>
    /// <param name="courses">The courses to evaluate.</param>
    /// <param name="controlRarityLookup">A frozen dictionary from which to lookup the rarity of a specific control.</param>
    /// <returns>The required courses ordered by control rarity.</returns>
    private ImmutableList<string>? FindRequiredCourseOrder(FrozenSet<Course> courses, FrozenDictionary<string, float> controlRarityLookup)
    {
        ImmutableList<CandidateSolution> beam = [CandidateSolution.Initial(controlRarityLookup)];

        while (beam.Count > 0)
        {
            var beamBuilder = new BeamBuilder<CandidateSolution>(BeamWidth, candidateSolutionComparer);

            foreach (var candidate in beam)
            {
                if (candidate.IsComplete)
                {
                    beamBuilder.Insert(candidate);
                    continue;
                }

                foreach (var course in courses)
                {
                    if (candidate.Courses.ContainsKey(course.Name))
                    {
                        continue;
                    }

                    var rarityGain = candidate.UnvisitedControls.CalculatePotentialRarityGain(
                        course.Controls,
                        controlRarityLookup,
                        MaximumRarity);

                    if (rarityGain <= 0.0F)
                    {
                        continue;
                    }

                    var projectedScore = candidate.RarityScore - rarityGain;
                    if (beamBuilder.IsFull && projectedScore >= beamBuilder.Worst()?.RarityScore)
                    {
                        continue;
                    }

                    var expanded = candidate.AddCourse(course, controlRarityLookup, MaximumRarity);
                    beamBuilder.Insert(expanded);
                }
            }

            beam = beamBuilder.ToImmutableList();
            if (beam.Count > 0 && beam[0].IsComplete)
            {
                break;
            }
        }

        if (beam.Count == 0)
        {
            return null;
        }

        return [.. beam[0].Courses
            .OrderBy(x => x.Value)
            .Select(x => x.Key)];
    }

    /// <summary>
    /// Computes if a course is dominated by another course by checking the course which shares its
    /// rarest control. A course without any controls is always considered dominated.
    /// </summary>
    /// <param name="course">The course to check.</param>
    /// <param name="coursesInvertedIndex">An inverted index of all courses with their controls as the key.</param>
    /// <param name="controlRarityLookup">A frozen dictionary from which to lookup the rarity of a specific control.</param>
    /// <returns>True if the course is dominated; otherwise false.</returns>
    private static bool IsDominated(
        Course course,
        ILookup<string, Course> coursesInvertedIndex,
        FrozenDictionary<string, float> controlRarityLookup)
    {
        var rarestControl = course.Controls.MaxBy(x => controlRarityLookup[x]);
        if (rarestControl is null)
        {
            return true;
        }

        return coursesInvertedIndex[rarestControl]
            .Where(y => y.Name != course.Name && y.Controls.Count > course.Controls.Count)
            .Any(y => course.Controls.IsSubsetOf(y.Controls));
    }
}
