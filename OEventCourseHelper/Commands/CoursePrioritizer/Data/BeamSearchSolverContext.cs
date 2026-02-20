using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record BeamSearchSolverContext(
    int TotalEventControlCount,
    float TotalControlRaritySum,
    int ControlMaskBucketCount,
    int CourseMaskBucketCount,
    ImmutableArray<Course> Courses,
    ImmutableArray<float> ControlRarityLookup,
    BitMask DominatedCoursesMask,
    ImmutableArray<ImmutableArray<ulong>> CourseInvertedIndex);
