using System.Collections.Immutable;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// An object containing the result of the solver.
/// </summary>
/// <param name="Mask"></param>
/// <param name="Order">All the event courses ordered by priority by the solver.</param>
internal record BeamSearchSolverResult(BitMask Mask, ImmutableList<Course> Order);
