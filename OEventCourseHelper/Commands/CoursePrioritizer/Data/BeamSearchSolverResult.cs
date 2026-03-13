using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record BeamSearchSolverResult(BitMask Mask, ImmutableList<Course> Order);
