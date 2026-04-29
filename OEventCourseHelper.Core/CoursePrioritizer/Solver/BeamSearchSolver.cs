using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.Data;
using System.Collections.Immutable;
using System.Data;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Core.CoursePrioritizer.Solver;

internal class BeamSearchSolver(int BeamWidth)
{
    public const ulong MaximumRarity = 10000000UL; // 7 zeroes provide similar precicion as a float.

    private static readonly CandidateBlueprint.RarityComparer candidateComparer = new();
    private static readonly CandidateBlueprint.TieBreakComparer tieBreakComparer = new();

    /// <summary>
    /// Uses a beam search to priotitize the courses in <paramref name="dataSet"/> and marking the courses
    /// that are required in order to visit all controls in the orienteering event.
    /// </summary>
    /// <remarks>
    /// <b>Data Contract:</b> For deterministic tie-breaking, the <paramref name="dataSet"/> must provide:
    /// <list type="bullet">
    /// <item>Courses sorted by <see cref="Course.ControlMask"/>, then alphabetically.</item>
    /// <item><see cref="Course.CourseIndex"/> matching the physical array index exactly.</item>
    /// </list>
    /// </remarks>
    /// <param name="dataSet">The data set to try and compute a solution for.</param>
    /// <returns>The solution found by the <see cref="BeamSearchSolver"/></returns>
    public BeamSearchSolverResult Solve(EventDataSet dataSet)
    {
        var context = CreateContext(dataSet);
        var requiredCoursesResult = PerformBeamSearch(context);
        if (requiredCoursesResult is null)
        {
            return new()
            {
                Success = false,
            };
        }

        return new()
        {
            Success = true,
            CourseMask = requiredCoursesResult.CourseMask,
            PriorityOrder = [
            .. requiredCoursesResult.CourseOrder
                .Select(x => new PrioritizedCourse(x.CourseName, true)),
            .. context.Courses
                .Where(x => !requiredCoursesResult.CourseMask[x.CourseIndex])
                .OrderBy(x => context.DominatedCoursesMask[x.CourseIndex])
                .ThenByDescending(x => x.ControlCount)
                .ThenBy(x => x.CourseName)
                .Select(x => new PrioritizedCourse(x.CourseName, false)),
            ]
        };
    }

    /// <summary>
    /// Computes the least amount of required courses and returns them in a prioritized order
    /// based on the rarity of the courses controls using a beam search algorithm.
    /// </summary>
    /// <param name="context">The context of the current search.</param>
    /// <returns>The required courses ordered by their respective priority.</returns>
    private PerformResult? PerformBeamSearch(BeamSearchSolverContext context)
    {
        var beamBuilder = new BeamBuilder(BeamWidth, candidateComparer, tieBreakComparer);
        var validCoursesMaskWorkspace = new BitMask.Workspace(context.CourseMaskBucketCount);
        var initialSolution = CandidateSolution.Initial(context);
        ImmutableArray<CandidateSolution> beam = [initialSolution];

        while (beam.Length > 0)
        {
            foreach (var candidate in beam)
            {
                if (candidate.IsComplete)
                {
                    beamBuilder.InsertOrDiscard(new CandidateBlueprint(candidate));
                    continue;
                }

                foreach (var controlIndex in candidate.UnvisitedControlsMask)
                {
                    var firstBucket = controlIndex * context.CourseMaskBucketCount;
                    var coursesWithControl = context.CourseInvertedIndex.Slice(firstBucket, context.CourseMaskBucketCount);
                    for (int i = 0; i < context.CourseMaskBucketCount; i++)
                    {
                        validCoursesMaskWorkspace.OrBucketAt(i, coursesWithControl);
                    }
                }

                for (int i = 0; i < context.CourseMaskBucketCount; i++)
                {
                    validCoursesMaskWorkspace.AndNotBucketAt(i, context.DominatedCoursesMask);
                    validCoursesMaskWorkspace.AndNotBucketAt(i, candidate.IncludedCoursesMask);
                }

                foreach (var courseIndex in validCoursesMaskWorkspace)
                {
                    var course = context.Courses[courseIndex];

                    var rarityGain = candidate.GetPotentialRarityGain(course, context.ControlRarityLookup);
                    if (rarityGain == 0UL)
                    {
                        continue;
                    }

                    var projectedScore = candidate.RarityScore - rarityGain;
                    if (beamBuilder.IsFull
                        &&
                        projectedScore >= beamBuilder.Worst.RarityScore)
                    {
                        continue;
                    }

                    var blueprint = new CandidateBlueprint(candidate, course, projectedScore);
                    beamBuilder.InsertOrDiscard(blueprint);
                }

                validCoursesMaskWorkspace.Clear();
            }

            beam = beamBuilder.MaterializeAndReset();
            if (beam.Length > 0 && beam[0].IsComplete)
            {
                break;
            }
        }

        if (beam.Length == 0)
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
        var courseInvertedIndexBuilder = new BitMask.Builder(dataSet.Controls.Length * courseMaskBucketCount);
        foreach (var course in dataSet.Courses)
        {
            foreach (var controlIndex in course.ControlMask)
            {
                controlFrequencies[controlIndex]++;
                var invertedIndexStart = (controlIndex * courseMaskBucketCount) << 6;
                courseInvertedIndexBuilder.Set(invertedIndexStart + course.CourseIndex);
            }
        }

        var totalControlRaritySum = 0UL;
        var targetControlsMaskBuilder = new BitMask.Builder(controlMaskBucketCount);
        var controlRarityLookup = new ulong[dataSet.Controls.Length];
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
            courseInvertedIndexBuilder.ToBitMask());
    }

    /// <summary>
    /// Calculates if the provided <paramref name="course"/> is dominated by any other <see cref="Course"/> in <paramref name="allCourses"/>.
    /// </summary>
    /// <param name="course">The <see cref="Course"> to check.</param>
    /// <param name="context">The context of the current search.</param>
    /// <returns>True if <paramref name="course"/> is dominated by any course mask in <paramref name="allCourses"/>; otherwise False.</returns>
    private static bool IsDominated(Course course, ImmutableArray<Course> courses, ReadOnlySpan<ulong> controlRarityLookup)
    {
        var rarestValue = 0UL;
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
                course.CourseIndex > other.CourseIndex)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A custom priority queue which limits the amount of blueprints to <paramref name="BeamWidth"/> and
    /// ensures only the best <see cref="CandidateBlueprint"/> are kept by using <paramref name="comparer"/>.
    /// </summary>
    /// <param name="BeamWidth">The maximum width of the beam.</param>
    /// <param name="comparer">The comparere to use.</param>
    /// <param name="tieBreaker">The comparer used to resolve tie breaks.</param>
    private class BeamBuilder(int BeamWidth, IComparer<CandidateBlueprint> comparer, IComparer<CandidateBlueprint>? tieBreaker = null)
    {
        private readonly CandidateBlueprint[] beam = new CandidateBlueprint[BeamWidth];

        public int Count { get; private set; } = 0;

        public bool IsFull => Count == BeamWidth;

        /// <summary>
        /// Inserts or discards an blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to insert.</param>
        /// <returns>True if the blueprint was keept; otherwise False.</returns>
        public bool InsertOrDiscard(CandidateBlueprint blueprint)
        {
            int index = Array.BinarySearch(beam, 0, Count, blueprint, comparer);
            if (index >= 0)
            {
                if (tieBreaker is not null
                    &&
                    tieBreaker.Compare(blueprint, beam[index]) < 0)
                {
                    beam[index] = blueprint;
                    return true;
                }

                return false;
            }

            index = ~index;
            if (index < BeamWidth)
            {
                var blueprintsToShift = Math.Min(Count, BeamWidth - 1) - index;
                if (blueprintsToShift > 0)
                {
                    Array.Copy(beam, index, beam, index + 1, blueprintsToShift);
                }

                beam[index] = blueprint;
                if (Count < BeamWidth)
                {
                    Count++;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the currently worst <see cref="CandidateBlueprint"/>.
        /// </summary>
        public CandidateBlueprint Worst => Count > 0 ? beam[Count - 1] : default;

        /// <summary>
        /// Creates an <see cref="ImmutableArray"/> containing the materialized <see cref="CandidateSolution"/>
        /// from the blueprints currenly in the builder, then resets the builder.
        /// </summary>
        public ImmutableArray<CandidateSolution> MaterializeAndReset()
        {
            var resultBuilder = ImmutableArray.CreateBuilder<CandidateSolution>(Count);
            for (int i = 0; i < Count; i++)
            {
                resultBuilder.Add(beam[i].Materialize());
                beam[i] = default;
            }

            Count = 0;
            return resultBuilder.MoveToImmutable();
        }
    }

    /// <summary>
    /// An object containing the result of the solver.
    /// </summary>
    /// <param name="CourseMask">A mask containing all required courses.</param>
    /// <param name="CourseOrder">All the event courses ordered by priority by the solver.</param>
    private record PerformResult(BitMask CourseMask, ImmutableList<Course> CourseOrder);
}
