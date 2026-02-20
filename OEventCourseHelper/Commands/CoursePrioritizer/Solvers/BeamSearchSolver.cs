using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Solvers;

internal class BeamSearchSolver(int BeamWidth)
{
    private static readonly CandidateSolution.RarityComparer candidateSolutionComparer = new();

    /// <summary>
    /// Uses a beam search to priotitize the courses in <paramref name="dataSet"/> and marking the courses
    /// that are required in order to visit all controls in the orienteering event.
    /// </summary>
    /// <param name="dataSet">The data set to try and compute a solution for.</param>
    /// <param name="solution">The computed solution.</param>
    /// <returns>True if a solution could be found; otherwise False</returns>
    public bool TrySolve(EventDataSet dataSet, [NotNullWhen(true)] out CourseResult[]? solution)
    {
        // Compute dominated courses
        var dominatedCourses = new List<Course>();
        var availableCourses = new List<Course>();
        var dominatedCourseIdMaskCache = new ulong[context.CourseIdMaskBucketCount];
        foreach (var course in context.CourseMasks)
        {
            if (IsDominated(course, context))
            {
                dominatedCourses.Add(course);
                BitMask.Set(dominatedCourseIdMaskCache, course.CourseIndex);
            }
            else
            {
                availableCourses.Add(course);
            }
        }

        var dominatedCourseIdMask = ImmutableCollectionsMarshal.AsImmutableArray(dominatedCourseIdMaskCache);

        // Perform the beam search to get the required courses ordered by rarity.
        var requiredCourses = FindRequiredCourseOrder(dominatedCourseIdMask, context);
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
    private ImmutableList<string>? FindRequiredCourseOrder(ImmutableArray<ulong> dominatedCourseIdMask, BeamSearchSolverContext context)
    {
        var initialSolution = CandidateSolution.Initial(context);
        ImmutableList<CandidateSolution> beam = [initialSolution];

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

                var validCoursesMask = new ulong[context.CourseIdMaskBucketCount];
                foreach (var controlIndex in candidate.UnvisitedControlsMask)
                {
                    var coursesWithControl = context.CourseInvertedIndex[controlIndex];
                    for (int i = 0; i < context.CourseIdMaskBucketCount; i++)
                    {
                        validCoursesMask[i] |= coursesWithControl[i];
                    }
                }

                foreach (var courseIndex in BitMask.Create(validCoursesMask))
                {
                    var course = context.CourseMasks[courseIndex];

                    if (candidate.IncludedCoursesMask[course.CourseIndex])
                    {
                        continue;
                    }

                    if (BitMask.IsSet(dominatedCourseIdMask.AsSpan(), course.CourseIndex))
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

                    var expanded = candidate.AddCourse(course, context);
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

        return [.. beam[0].CourseOrder.Select(x => x.CourseName)];
    }

    /// <summary>
    /// Calculates if the provided <paramref name="course"/> is dominated by any other <see cref="Course"/> in <paramref name="allCourses"/>.
    /// </summary>
    /// <param name="course">The <see cref="Course"> to check.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>True if <paramref name="course"/> is dominated by any course mask in <paramref name="allCourses"/>; otherwise False.</returns>
    private static bool IsDominated(Course course, BeamSearchSolverContext context)
    {
        var rarestValue = -1.0F;
        var indexOfRarest = -1;
        foreach (var controlIndex in course.ControlMask)
        {
            if (controlIndex >= context.ControlRarityLookup.Length)
            {
                continue;
            }

            if (rarestValue < context.ControlRarityLookup[controlIndex])
            {
                rarestValue = context.ControlRarityLookup[controlIndex];
                indexOfRarest = controlIndex;
            }
        }

        if (indexOfRarest == -1)
        {
            return true;
        }

        foreach (var other in context.CourseMasks)
        {
            if (ReferenceEquals(course, other))
            {
                continue;
            }

            if (other.ControlMask[indexOfRarest])
            {
                if (course.ControlMask.IsSubsetOf(other.ControlMask))
                {
                    if (!course.ControlMask.IsIdenticalTo(other.ControlMask)
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
