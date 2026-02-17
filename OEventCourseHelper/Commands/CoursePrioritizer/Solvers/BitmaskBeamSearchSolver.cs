using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Solvers;

internal class BitmaskBeamSearchSolver(int BeamWidth)
{
    private static readonly BitmaskCandidateSolution.RarityComparer candidateSolutionComparer = new();

    /// <summary>
    /// Uses a beam search to priotitize the courses in <paramref name="context"/> and marking the courses that are required
    /// in order to visit all controls in the orienteering event.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <param name="solution">The computed solution.</param>
    /// <returns>True if a solution could be found; otherwise False</returns>
    public bool TrySolve(BitmaskBeamSearchSolverContext context, [NotNullWhen(true)] out CourseResult[]? solution)
    {
        // Compute dominated courses
        var dominatedCourses = new List<CourseMask>();
        var availableCourses = new List<CourseMask>();
        foreach (var course in context.CourseMasks)
        {
            if (IsDominated(course, context))
            {
                dominatedCourses.Add(course);
            }
            else
            {
                availableCourses.Add(course);
            }
        }

        // Perform the beam search to get the required courses ordered by rarity.
        var requiredCourses = FindRequiredCourseOrder(availableCourses, context);
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
    /// Computes the least amount of required courses and returns them in a prioritized order
    /// based on the rarity of the courses controls using a beam search algorithm.
    /// </summary>
    /// <param name="courses">The set containing all course masks.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>The required courses ordered by their respective priority.</returns>
    private ImmutableList<string>? FindRequiredCourseOrder(IEnumerable<CourseMask> courses, BitmaskBeamSearchSolverContext context)
    {
        var initialSolution = BitmaskCandidateSolution.Initial(context);
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

                    var rarityGain = candidate.GetPotentialRarityGain(course, context.ControlRarityLookup);
                    if (rarityGain <= 0.0F)
                    {
                        continue;
                    }

                    var projectedScore = candidate.RarityScore - rarityGain;
                    if (beamBuilder.IsFull && projectedScore >= beamBuilder.Worst()?.RarityScore)
                    {
                        continue;
                    }

                    var expanded = candidate.AddCourse(course, context.ControlRarityLookup);
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

    /// <summary>
    /// Calculates if the provided <paramref name="course"/> is dominated by any other <see cref="CourseMask"/> in <paramref name="allCourses"/>.
    /// </summary>
    /// <param name="course">The <see cref="CourseMask"> to check.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>True if <paramref name="course"/> is dominated by any course mask in <paramref name="allCourses"/>; otherwise False.</returns>
    private static bool IsDominated(CourseMask course, BitmaskBeamSearchSolverContext context)
    {
        var seeker = new RarestSeeker(context.ControlRarityLookup);
        course.ForEachControl(ref seeker);

        if (seeker.IndexOfRarest == -1)
        {
            return true;
        }

        int bucketIndex = seeker.IndexOfRarest >> 6;
        ulong bitMask = 1UL << (seeker.IndexOfRarest & 63);

        foreach (var other in context.CourseMasks)
        {
            if (ReferenceEquals(course, other))
            {
                continue;
            }

            if ((other.ControlMask[bucketIndex] & bitMask) != 0)
            {
                if (course.IsSubsetOf(other))
                {
                    if (!course.IsIdenticalTo(other)
                        ||
                        string.CompareOrdinal(course.CourseName, other.CourseName) > 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// A custom priority queue which limits the amount of items to <paramref name="BeamWidth"/> and
    /// ensures only the best <see cref="T"> are kept by using <paramref name="comparer"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="BeamWidth">The maximum width of the beam.</param>
    /// <param name="comparer">The comparere to use.</param>
    private class BeamBuilder<T>(int BeamWidth, IComparer<T> comparer)
    {
        private readonly List<T> beam = new(BeamWidth);

        public int Count => beam.Count;

        public bool IsFull => beam.Count == BeamWidth;

        /// <summary>
        /// Inserts or discards an item.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <returns>True if the item was keept; otherwise False.</returns>
        public bool Insert(T item)
        {
            int index = beam.BinarySearch(item, comparer);

            if (index < 0)
            {
                index = ~index;
            }

            if (index < BeamWidth)
            {
                beam.Insert(index, item);

                if (beam.Count > BeamWidth)
                {
                    beam.RemoveAt(BeamWidth);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the currently worst item.
        /// </summary>
        public T? Worst() => beam.Count > 0 ? beam[^1] : default;

        /// <summary>
        /// Creates an <see cref="ImmutableList{T}"/> of the items currenly in the builder.
        /// </summary>
        public ImmutableList<T> ToImmutableList() => [.. beam];
    }

    public record CourseResult(string Name, bool IsRequired);
}
