using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// An object containing the result of the solver.
/// </summary>
internal record BeamSearchSolverResult
{
    [MemberNotNullWhen(true, nameof(CourseMask), nameof(PriorityOrder))]
    public bool Success { get; init; }

    /// <summary>
    /// A mask containing all required courses.
    /// </summary>
    public BitMask? CourseMask { get; init; }

    /// <summary>
    /// All the event courses ordered by priority by the solver.
    /// </summary>
    public ImmutableArray<PrioritizedCourse> PriorityOrder { get; init; }
}

internal record PrioritizedCourse(string CourseName, bool IsRequired);
