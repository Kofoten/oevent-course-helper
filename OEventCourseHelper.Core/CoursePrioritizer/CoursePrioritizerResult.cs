using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using System.Collections.Immutable;

namespace OEventCourseHelper.Core.CoursePrioritizer;

public record CoursePrioritizerResult
{
    private protected CoursePrioritizerResult() { }

    public sealed record Success(
        ImmutableArray<PrioritizedCourse> PriorityOrder,
        CoursePrioritizerEngine.Summary Summary,
        CoursePrioritizerEngine.ValidationInfo ValidationInfo) : CoursePrioritizerResult;

    public sealed record ParseStreamFailure(IReadOnlyList<string> Errors) : CoursePrioritizerResult;
    public sealed record ValidationFailure(CoursePrioritizerEngine.ValidationInfo ValidationInfo) : CoursePrioritizerResult;
    public sealed record NoSolutionFound(CoursePrioritizerEngine.ValidationInfo ValidationInfo) : CoursePrioritizerResult;
}
