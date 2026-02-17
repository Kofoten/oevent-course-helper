using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Commands.CoursePrioritizer.Solvers;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitmaskBeamSearchSolverTests
{
    [Fact]
    public void TrySolve_ShouldOrderCoursesCorrectly()
    {
        // Setup
        var courseMasks = new CourseMask[]
        {
            new("Dominated", [5UL], 2), // Mask 101000
            new("Longest", [21Ul], 3),  // Mask 101010
            new("Control", [18Ul], 2),  // Mask 010010
            new("Rarest", [40UL], 2),   // Mask 000101
        };

        ImmutableArray<float> controlRarityLookup = [0.5F, 1.0F, 0.5F, 1.0F, 0.5F, 1.0F];

        var solver = new BitmaskBeamSearchSolver(3);
        var context = new BitmaskBeamSearchSolverContext(
            6,
            controlRarityLookup.Sum(),
            1,
            courseMasks.ToFrozenSet(),
            controlRarityLookup);

        // Act
        var solutionFound = solver.TrySolve(context, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual!.Length.Should().Be(4);
        actual[0].Should().Be(new BitmaskBeamSearchSolver.CourseResult("Rarest", true));
        actual[1].Should().Be(new BitmaskBeamSearchSolver.CourseResult("Longest", true));
        actual[2].Should().Be(new BitmaskBeamSearchSolver.CourseResult("Control", true));
        actual[3].Should().Be(new BitmaskBeamSearchSolver.CourseResult("Dominated", false));
    }
}
