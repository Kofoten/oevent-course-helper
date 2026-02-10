using Microsoft.Extensions.Logging;
using PrioritiseTestRunCourses.Data;
using PrioritiseTestRunCourses.Extensions;
using PrioritiseTestRunCourses.Logging;
using PrioritiseTestRunCourses.Xml;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace PrioritiseTestRunCourses;

internal class Runtime(Options options, ILogger logger)
{
    private const float MaximumRarity = 1.0F;

    private readonly CandidateSolution.RarityPriorityComparer candidateSolutionRarityComparer = new();

    public Result<CourseResult[], ErrorCode> Run()
    {
        var iofReader = IOFXmlReader.Create();
        if (!iofReader.TryLoad(options.IOFXmlFilePath, out var courseData, out var errors))
        {
            logger.FailedToLoadFile(options.IOFXmlFilePath, errors.FormatErrors());
            return new Failure<CourseResult[], ErrorCode>(ErrorCode.FailedToLoadFile);
        }

        // Convert and filter the IOF data types to a simpler data set.
        var courses = courseData.RaceCourseData
            .SelectMany(x => x.Course, (_, y) => Course.FromIOF(y))
            .Where(x => options.Filters.Count == 0 || options.Filters.Any(y => x.Name.Contains(y)))
            .Where(x => x.Controls.Count > 0)
            .ToFrozenSet();

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
            logger.NoSolutionFound();
            return new Failure<CourseResult[], ErrorCode>(ErrorCode.NoSolutionFound);
        }

        // Combine the lists/sets into the final result.
        var requiredCoursesSet = requiredCourses.ToFrozenSet();
        CourseResult[] result = [
            .. requiredCourses.Select(x => new CourseResult(x, true)),
            .. availableCourses
                .Where(x => !requiredCoursesSet.Contains(x.Name))
                .OrderByDescending(x => x.Controls.Count)
                .ThenBy(x => x.Name)
                .Select(x => new CourseResult(x.Name, false)),
            .. dominatedCourses.Order().Select(x => new CourseResult(x, false)),
        ];

        return new Success<CourseResult[], ErrorCode>(result);
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
        List<CandidateSolution> beam = [CandidateSolution.Initial(controlRarityLookup)];
        while (beam.Count > 0)
        {
            // Compute the new top candidates by adding all completed and new candidates to the priority queue.
            var topCandidates = new PriorityQueue<CandidateSolution, CandidateSolution>(candidateSolutionRarityComparer);
            foreach (var candidate in beam)
            {
                if (candidate.IsComplete)
                {
                    topCandidates.Enqueue(candidate, candidate);
                    continue;
                }

                foreach (var expanded in ExpandCandidate(candidate, courses, controlRarityLookup))
                {
                    topCandidates.Enqueue(expanded, expanded);
                }
            }

            // Prune the candidates and populate a new beam.
            beam = [];
            while (beam.Count < options.BeamWidth && topCandidates.Count > 0)
            {
                beam.Add(topCandidates.Dequeue());
            }

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
    /// Expands a candidate by creating all possible following candidates.
    /// </summary>
    /// <param name="candidate">The candidate to expand.</param>
    /// <param name="courses">The courses that are being evaluated.</param>
    /// <returns>All possible candidates that can follow <paramref name="candidate"/>.</returns>
    private static IEnumerable<CandidateSolution> ExpandCandidate(
        CandidateSolution candidate,
        FrozenSet<Course> courses,
        FrozenDictionary<string, float> controlRarityLookup)
    {
        foreach (var course in courses)
        {
            if (candidate.Courses.ContainsKey(course.Name))
            {
                continue;
            }

            var newUnvisitedControls = candidate.UnvisitedControls.Except(course.Controls);
            var newRarityScore = newUnvisitedControls.Sum(x => controlRarityLookup.GetValueOrDefault(x, MaximumRarity));

            yield return new CandidateSolution(
                candidate.Courses.Add(course.Name, candidate.Courses.Count),
                newUnvisitedControls,
                newRarityScore);
        }
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
