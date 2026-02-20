using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Solvers;

internal class BeamSearchSolver(int BeamWidth)
{
    public const float MaximumRarity = 1.0F;

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
        var context = BuildContext(dataSet);
        var requiredCoursesResult = FindAndOrderRequiredCourses(context);
        if (requiredCoursesResult is null)
        {
            solution = null;
            return false;
        }

        solution = [
            .. requiredCoursesResult.Order
                .Select(x => new CourseResult(x.CourseName, true)),
            .. context.Courses
                .Where(x => !requiredCoursesResult.Mask[x.CourseIndex])
                .OrderBy(x => context.DominatedCoursesMask[x.CourseIndex])
                .ThenByDescending(x => x.ControlCount)
                .ThenBy(x => x.CourseName)
                .Select(x => new CourseResult(x.CourseName, false)),
        ];

        return true;
    }

    /// <summary>
    /// Computes the least amount of required courses and returns them in a prioritized order
    /// based on the rarity of the courses controls using a beam search algorithm.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>The required courses ordered by their respective priority.</returns>
    private RequiredCoursesResult? FindAndOrderRequiredCourses(BeamSearchSolverContext context)
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

                var validCoursesMask = new ulong[context.CourseMaskBucketCount];
                foreach (var controlIndex in candidate.UnvisitedControlsMask)
                {
                    var coursesWithControl = context.CourseInvertedIndex[controlIndex];
                    for (int i = 0; i < context.CourseMaskBucketCount; i++)
                    {
                        validCoursesMask[i] |= coursesWithControl[i];
                    }
                }

                foreach (var courseIndex in BitMask.Create(validCoursesMask))
                {
                    var course = context.Courses[courseIndex];

                    if (candidate.IncludedCoursesMask[course.CourseIndex])
                    {
                        continue;
                    }

                    if (context.DominatedCoursesMask[course.CourseIndex])
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

        return new(beam[0].IncludedCoursesMask, beam[0].CourseOrder);
    }

    /// <summary>
    /// Builds a new <see cref="BeamSearchSolverContext"/> from <paramref name="dataSet"/>.
    /// </summary>
    /// <param name="dataSet">The data set from wich to build the context.</param>
    /// <returns>A new instance of <see cref="BeamSearchSolverContext"/>.</returns>
    public static BeamSearchSolverContext BuildContext(EventDataSet dataSet)
    {
        var courseMaskBucketCount = BitMask.GetBucketCount(dataSet.Courses.Length);
        var controlFrequencies = new int[dataSet.TotalEventControlCount];
        var courseInvertedIndex = new ulong[dataSet.TotalEventControlCount][];
        foreach (var course in dataSet.Courses)
        {
            foreach (var controlIndex in course.ControlMask)
            {
                controlFrequencies[controlIndex]++;

                if (courseInvertedIndex[controlIndex] is null
                    ||
                    courseInvertedIndex[controlIndex].Length == 0)
                {
                    courseInvertedIndex[controlIndex] = new ulong[courseMaskBucketCount];
                }

                BitMask.Set(courseInvertedIndex[controlIndex], course.CourseIndex);
            }
        }

        var totalControlRaritySum = 0.0F;
        var controlRarityLookup = new float[dataSet.TotalEventControlCount];
        var immutableCourseInvertedIndicies = new ImmutableArray<ulong>[dataSet.TotalEventControlCount];
        for (int i = 0; i < dataSet.TotalEventControlCount; i++)
        {
            if (controlFrequencies[i] == 0)
            {
                controlRarityLookup[i] = 0.0F;
            }
            else
            {
                controlRarityLookup[i] = MaximumRarity / controlFrequencies[i];
                totalControlRaritySum += controlRarityLookup[i];
            }

            immutableCourseInvertedIndicies[i] = ImmutableCollectionsMarshal.AsImmutableArray(courseInvertedIndex[i]);
        }

        var dominatedCoursesMask = new ulong[courseMaskBucketCount];
        foreach (var course in dataSet.Courses)
        {
            if (IsDominated(course, dataSet.Courses, controlRarityLookup))
            {
                BitMask.Set(dominatedCoursesMask, course.CourseIndex);
            }
        }

        return new BeamSearchSolverContext(
            dataSet.TotalEventControlCount,
            totalControlRaritySum,
            BitMask.GetBucketCount(dataSet.TotalEventControlCount),
            courseMaskBucketCount,
            dataSet.Courses,
            ImmutableCollectionsMarshal.AsImmutableArray(controlRarityLookup),
            BitMask.Create(dominatedCoursesMask),
            ImmutableCollectionsMarshal.AsImmutableArray(immutableCourseInvertedIndicies));
    }

    /// <summary>
    /// Calculates if the provided <paramref name="course"/> is dominated by any other <see cref="Course"/> in <paramref name="allCourses"/>.
    /// </summary>
    /// <param name="course">The <see cref="Course"> to check.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>True if <paramref name="course"/> is dominated by any course mask in <paramref name="allCourses"/>; otherwise False.</returns>
    private static bool IsDominated(Course course, ImmutableArray<Course> courses, ReadOnlySpan<float> controlRarityLookup)
    {
        var rarestValue = -1.0F;
        var indexOfRarest = -1;
        foreach (var controlIndex in course.ControlMask)
        {
            if (controlIndex >= controlRarityLookup.Length)
            {
                continue;
            }

            if (rarestValue < controlRarityLookup[controlIndex])
            {
                rarestValue = controlRarityLookup[controlIndex];
                indexOfRarest = controlIndex;
            }
        }

        if (indexOfRarest == -1)
        {
            return true;
        }

        foreach (var other in courses)
        {
            if (ReferenceEquals(course, other))
            {
                continue;
            }

            if (!other.ControlMask[indexOfRarest])
            {
                continue;
            }

            if (!course.ControlMask.IsSubsetOf(other.ControlMask))
            {
                continue;
            }

            if (!course.ControlMask.IsIdenticalTo(other.ControlMask)
                ||
                string.CompareOrdinal(course.CourseName, other.CourseName) > 0)
            {
                return true;
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
