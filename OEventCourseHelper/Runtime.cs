using Microsoft.Extensions.Logging;
using OEventCourseHelper.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Xml;
using OEventCourseHelper.Xml.NodeReaders;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper;

internal class Runtime(Options options, ILogger logger)
{
    private const float MaximumRarity = 1.0F;

    private static readonly CandidateSolution.RarityPriorityComparer candidateSolutionComparer = new();

    public Result<CourseResult[], ErrorCode> Run()
    {
        var iofReader = IOFXmlReader.Create();
        var courseReader = new CourseNodeReader();
        if (!iofReader.TryStream(options.IOFXmlFilePath, courseReader, out var errors))
        {
            logger.FailedToLoadFile(options.IOFXmlFilePath, errors.FormatErrors());
            return new Failure<CourseResult[], ErrorCode>(ErrorCode.FailedToLoadFile);
        }

        // Convert and filter the IOF data types to a simpler data set.
        var courses = courseReader.Courses
            .Where(x => x.Controls.Count > 0)
            .Where(x => options.Filters.Count == 0 || options.Filters.Any(y => x.Name.Contains(y)))
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
        ImmutableList<CandidateSolution> beam = [CandidateSolution.Initial(controlRarityLookup)];

        while (beam.Count > 0)
        {
            var beamBuilder = new BeamBuilder<CandidateSolution>(options.BeamWidth, candidateSolutionComparer);

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
