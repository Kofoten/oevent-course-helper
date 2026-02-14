using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Solvers;

internal class BitmaskBeamSearchSolver(int BeamWidth, int TotalEventControlCount)
{
    private const float MaximumRarity = 1.0F;

    private static readonly BitmaskCandidateSolution.RarityComparer candidateSolutionComparer = new();

    /// <summary>
    /// Uses a beam search to find an optimal sequence of required courses based on control rarity.
    /// </summary>
    /// <param name="courses">The courses to prioritize.</param>
    /// <param name="solution">The computed solution.</param>
    /// <returns>True if a solution could be found; otherwise False</returns>
    public bool TrySolve(FrozenSet<CourseMask> courses, [NotNullWhen(true)] out CourseResult[]? solution)
    {
        // Create the control rarity lookup.
        var controlRarityLookup = BuildControlRarityLookup(courses);

        // Compute dominated courses
        var dominatedCourses = new List<CourseMask>();
        var availableCourses = new List<CourseMask>();
        foreach (var course in courses)
        {
            if (IsDominated(course, courses, controlRarityLookup))
            {
                dominatedCourses.Add(course);
            }
            else
            {
                availableCourses.Add(course);
            }
        }

        // Perform the beam search to get the required courses ordered by rarity.
        var requiredCourses = FindRequiredCourseOrder(availableCourses, controlRarityLookup);
        if (requiredCourses is null)
        {
            solution = null;
            return false;
        }

        // Combine the lists/sets into the final result.
        var requiredCoursesSet = requiredCourses.ToFrozenSet();
        solution = [
            .. requiredCourses.Select(x => new CourseResult(x, true)),
            .. availableCourses
                .Where(x => !requiredCoursesSet.Contains(x.CourseName))
                .OrderByDescending(x => x.ControlCount)
                .ThenBy(x => x.CourseName)
                .Select(x => new CourseResult(x.CourseName, false)),
            .. dominatedCourses
                .OrderByDescending(x => x.ControlCount)
                .ThenBy(x => x.CourseName)
                .Select(x => new CourseResult(x.CourseName, false)),
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
    private ImmutableList<string>? FindRequiredCourseOrder(IEnumerable<CourseMask> courses, ImmutableArray<float> controlRarityLookup)
    {
        var initialSolution = BitmaskCandidateSolution.Initial(TotalEventControlCount, controlRarityLookup);
        ImmutableList<BitmaskCandidateSolution> beam = [initialSolution];

        while (beam.Count > 0)
        {
            var beamBuilder = new BeamBuilder<BitmaskCandidateSolution>(BeamWidth, candidateSolutionComparer);

            foreach (var candidate in beam)
            {
                if (candidate.IsComplete)
                {
                    beamBuilder.Insert(candidate);
                    continue;
                }

                foreach (var course in courses)
                {
                    if (candidate.Courses.Contains(course.CourseName))
                    {
                        continue;
                    }

                    var rarityGain = candidate.GetPotentialRarityGain(course, controlRarityLookup);
                    if (rarityGain <= 0.0F)
                    {
                        continue;
                    }

                    var projectedScore = candidate.RarityScore - rarityGain;
                    if (beamBuilder.IsFull && projectedScore >= beamBuilder.Worst()?.RarityScore)
                    {
                        continue;
                    }

                    var expanded = candidate.AddCourse(course, controlRarityLookup);
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

        return beam[0].Courses;
    }

    private static bool IsDominated(
        CourseMask currentCourse,
        FrozenSet<CourseMask> allCourses,
        ImmutableArray<float> controlRarityLookup)
    {
        var seeker = new RarestSeeker(controlRarityLookup);
        currentCourse.ForEachControl(ref seeker);

        if (seeker.IndexOfRarest == -1)
        {
            return true;
        }

        int bucketIndex = seeker.IndexOfRarest >> 6;
        ulong bitMask = 1UL << (seeker.IndexOfRarest & 63);

        foreach (var other in allCourses)
        {
            if (ReferenceEquals(currentCourse, other))
            {
                continue;
            }

            if ((other.ControlMask[bucketIndex] & bitMask) != 0)
            {
                if (currentCourse.IsSubsetOf(other))
                {
                    if (!currentCourse.IsIdenticalTo(other)
                        ||
                        string.CompareOrdinal(currentCourse.CourseName, other.CourseName) > 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private ImmutableArray<float> BuildControlRarityLookup(IEnumerable<CourseMask> courses)
    {
        var controlFrequency = new int[TotalEventControlCount];
        var counter = new RarityCounter { Counts = controlFrequency };
        foreach (var course in courses)
        {
            course.ForEachControl(ref counter);
        }

        var rarityLookupBuilder = ImmutableArray.CreateBuilder<float>(TotalEventControlCount);
        for (int i = 0; i < TotalEventControlCount; i++)
        {
            rarityLookupBuilder.Add(controlFrequency[i] > 0 ? MaximumRarity / controlFrequency[i] : 0f);
        }

        return rarityLookupBuilder.DrainToImmutable();
    }
}
