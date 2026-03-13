using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record BeamSearchSolverContext(
    int TotalEventControlCount,
    ulong TotalControlRaritySum,
    int ControlMaskBucketCount,
    int CourseMaskBucketCount,
    ImmutableArray<Course> Courses,
    ImmutableArray<ulong> ControlRarityLookup,
    BitMask DominatedCoursesMask,
    ImmutableArray<BitMask> CourseInvertedIndex);
