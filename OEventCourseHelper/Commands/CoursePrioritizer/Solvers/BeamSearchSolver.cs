using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Solvers;

internal class BeamSearchSolver(int BeamWidth)
{
    public const ulong MaximumRarity = 10000000UL; // 7 zeroes provide similar precicion as a float.

    private static readonly CandidateBlueprint.RarityComparer candidateComparer = new();
    private static readonly CandidateBlueprint.TieBreakComparer tieBreakComparer = new();

    /// <summary>
    /// Uses a beam search to priotitize the courses in <paramref name="dataSet"/> and marking the courses
    /// that are required in order to visit all controls in the orienteering event.
    /// </summary>
    /// <param name="dataSet">The data set to try and compute a solution for.</param>
    /// <param name="solution">The computed solution.</param>
    /// <returns>True if a solution could be found; otherwise False</returns>
    public bool TrySolve(EventDataSet dataSet, [NotNullWhen(true)] out PriorityResult[]? solution)
    {
        var context = CreateContext(dataSet);
        var requiredCoursesResult = PerformBeamSearch(context);
        if (requiredCoursesResult is null)
        {
            solution = null;
            return false;
        }

        solution = [
            .. requiredCoursesResult.Order
                .Select(x => new PriorityResult(x.CourseName, true)),
            .. context.Courses
                .Where(x => !requiredCoursesResult.Mask[x.CourseIndex])
                .OrderBy(x => context.DominatedCoursesMask[x.CourseIndex])
                .ThenByDescending(x => x.ControlCount)
                .ThenBy(x => x.CourseName)
                .Select(x => new PriorityResult(x.CourseName, false)),
        ];

        return true;
    }

    /// <summary>
    /// Computes the least amount of required courses and returns them in a prioritized order
    /// based on the rarity of the courses controls using a beam search algorithm.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>The required courses ordered by their respective priority.</returns>
    private BeamSearchSolverResult? PerformBeamSearch(BeamSearchSolverContext context)
    {
        var validCoursesMaskWorkspace = new BitMask.Workspace(context.CourseMaskBucketCount);
        var initialSolution = CandidateSolution.Initial(context);
        ImmutableList<CandidateSolution> beam = [initialSolution];

        while (beam.Count > 0)
        {
            var beamBuilder = new BeamBuilder<CandidateBlueprint>(BeamWidth, candidateComparer, tieBreakComparer);

            foreach (var candidate in beam)
            {
                if (candidate.IsComplete)
                {
                    beamBuilder.InsertOrDiscard(new CandidateBlueprint(candidate));
                    continue;
                }

                foreach (var controlIndex in candidate.UnvisitedControlsMask)
                {
                    var coursesWithControl = context.CourseInvertedIndex[controlIndex];
                    for (int i = 0; i < context.CourseMaskBucketCount; i++)
                    {
                        validCoursesMaskWorkspace.OrBucketAt(i, coursesWithControl);
                        validCoursesMaskWorkspace.AndNotBucketAt(i, context.DominatedCoursesMask);
                        validCoursesMaskWorkspace.AndNotBucketAt(i, candidate.IncludedCoursesMask);
                    }
                }

                foreach (var courseIndex in validCoursesMaskWorkspace)
                {
                    var course = context.Courses[courseIndex];

                    var rarityGain = candidate.GetPotentialRarityGain(course, context.ControlRarityLookup);
                    if (rarityGain <= 0.0F)
                    {
                        continue;
                    }

                    var projectedScore = candidate.RarityScore - rarityGain;
                    if (beamBuilder.IsFull && projectedScore >= beamBuilder.Worst.RarityScore)
                    {
                        continue;
                    }

                    var blueprint = new CandidateBlueprint(candidate, course, projectedScore);
                    beamBuilder.InsertOrDiscard(blueprint);
                }

                validCoursesMaskWorkspace.Clear();
            }

            beam = beamBuilder.ToImmutableList(x => x.Materialize());
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
    /// Creates a new <see cref="BeamSearchSolverContext"/> from <paramref name="dataSet"/>.
    /// </summary>
    /// <param name="dataSet">The data set from wich to build the context.</param>
    /// <returns>A new instance of <see cref="BeamSearchSolverContext"/>.</returns>
    public static BeamSearchSolverContext CreateContext(EventDataSet dataSet)
    {
        var controlMaskBucketCount = BitMask.GetBucketCount(dataSet.Controls.Length);
        var courseMaskBucketCount = BitMask.GetBucketCount(dataSet.Courses.Length);
        var controlFrequencies = new ulong[dataSet.Controls.Length];
        var courseInvertedIndex = new BitMask.Builder[dataSet.Controls.Length];
        foreach (var course in dataSet.Courses)
        {
            foreach (var controlIndex in course.ControlMask)
            {
                controlFrequencies[controlIndex]++;

                if (courseInvertedIndex[controlIndex] is null)
                {
                    courseInvertedIndex[controlIndex] = new BitMask.Builder(courseMaskBucketCount);
                }

                courseInvertedIndex[controlIndex].Set(course.CourseIndex);
            }
        }

        var totalControlRaritySum = 0UL;
        var targetControlsMaskBuilder = new BitMask.Builder(controlMaskBucketCount);
        var controlRarityLookup = new ulong[dataSet.Controls.Length];
        var immutableCourseInvertedIndicies = new BitMask[dataSet.Controls.Length];
        for (int i = 0; i < dataSet.Controls.Length; i++)
        {
            if (controlFrequencies[i] == 0)
            {
                controlRarityLookup[i] = 0UL;
            }
            else
            {
                targetControlsMaskBuilder.Set(i);
                controlRarityLookup[i] = MaximumRarity / controlFrequencies[i];
                totalControlRaritySum += controlRarityLookup[i];
            }

            if (courseInvertedIndex[i] is null)
            {
                immutableCourseInvertedIndicies[i] = new BitMask.Builder(courseMaskBucketCount).ToBitMask();
            }
            else
            {
                immutableCourseInvertedIndicies[i] = courseInvertedIndex[i].ToBitMask();
            }
        }

        var dominatedCoursesMaskBuilder = new BitMask.Builder(courseMaskBucketCount);
        foreach (var course in dataSet.Courses)
        {
            if (IsDominated(course, dataSet.Courses, controlRarityLookup))
            {
                dominatedCoursesMaskBuilder.Set(course.CourseIndex);
            }
        }

        return new BeamSearchSolverContext(
            targetControlsMaskBuilder.ToBitMask(),
            totalControlRaritySum,
            BitMask.GetBucketCount(dataSet.Controls.Length),
            courseMaskBucketCount,
            dataSet.Courses,
            ImmutableCollectionsMarshal.AsImmutableArray(controlRarityLookup),
            dominatedCoursesMaskBuilder.ToBitMask(),
            ImmutableCollectionsMarshal.AsImmutableArray(immutableCourseInvertedIndicies));
    }

    /// <summary>
    /// Calculates if the provided <paramref name="course"/> is dominated by any other <see cref="Course"/> in <paramref name="allCourses"/>.
    /// </summary>
    /// <param name="course">The <see cref="Course"> to check.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>True if <paramref name="course"/> is dominated by any course mask in <paramref name="allCourses"/>; otherwise False.</returns>
    private static bool IsDominated(Course course, ImmutableArray<Course> courses, ReadOnlySpan<ulong> controlRarityLookup)
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

            if (!course.ControlMask.Equals(other.ControlMask)
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
    /// ensures only the best <typeparamref name="T"/> are kept by using <paramref name="comparer"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="BeamWidth">The maximum width of the beam.</param>
    /// <param name="comparer">The comparere to use.</param>
    /// <param name="tieBreaker">The comparer used to resolve tie breaks.</param>
    private class BeamBuilder<T>(int BeamWidth, IComparer<T> comparer, IComparer<T>? tieBreaker = null)
    {
        private readonly List<T> beam = new(BeamWidth);

        public int Count => beam.Count;

        public bool IsFull => beam.Count == BeamWidth;

        /// <summary>
        /// Inserts or discards an item.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <returns>True if the item was keept; otherwise False.</returns>
        public bool InsertOrDiscard(T item)
        {
            int index = beam.BinarySearch(item, comparer);
            if (index >= 0)
            {
                if (tieBreaker is not null
                    &&
                    tieBreaker.Compare(item, beam[index]) < 0)
                {
                    beam[index] = item;
                    return true;
                }

                return false;
            }

            index = ~index;
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
        public T? Worst => beam.Count > 0 ? beam[^1] : default;

        /// <summary>
        /// Creates an <see cref="ImmutableList{T}"/> of the items currenly in the builder.
        /// </summary>
        public ImmutableList<T> ToImmutableList() => [.. beam];

        /// <summary>
        /// Creates an <see cref="ImmutableList{T}"/> of the items currenly in the builder.
        /// </summary>
        public ImmutableList<TResult> ToImmutableList<TResult>(Func<T, TResult> selector)
        {
            var resultBuilder = ImmutableList.CreateBuilder<TResult>();
            foreach (var item in beam)
            {
                resultBuilder.Add(selector(item));
            }

            return resultBuilder.ToImmutableList();
        }
    }

    public record PriorityResult(string Name, bool IsRequired);
}
