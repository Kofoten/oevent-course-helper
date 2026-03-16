using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// A context to hold data computed by the pre-processing step.
/// </summary>
/// <param name="TotalEventControlCount">The total number of controls in the event.</param>
/// <param name="TotalControlRaritySum">The total sum of rarities of the controls in the event.</param>
/// <param name="ControlMaskBucketCount">The number of buckets required for a <see cref="BitMask"/> of controls.</param>
/// <param name="CourseMaskBucketCount">The number of buckets required for a <see cref="BitMask"/> of course indicies.</param>
/// <param name="Courses">All courses included in the event.</param>
/// <param name="ControlRarityLookup">The lookup of the rarity of a specific control.</param>
/// <param name="DominatedCoursesMask">A <see cref="BitMask"/> covering the indicies of all dominated courses.</param>
/// <param name="CourseInvertedIndex">An inverted index containg a <see cref="BitMask"/> containing the indicies of each course covering a specific control.</param>
internal record BeamSearchSolverContext(
    int TotalEventControlCount,
    ulong TotalControlRaritySum,
    int ControlMaskBucketCount,
    int CourseMaskBucketCount,
    ImmutableArray<Course> Courses,
    ImmutableArray<ulong> ControlRarityLookup,
    BitMask DominatedCoursesMask,
    ImmutableArray<BitMask> CourseInvertedIndex);
