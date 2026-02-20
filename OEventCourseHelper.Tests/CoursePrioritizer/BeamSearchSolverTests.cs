using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Commands.CoursePrioritizer.Solvers;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BeamSearchSolverTests
{
    [Fact]
    public void TrySolve_ShouldOrderCoursesCorrectly()
    {
        // Setup
        var courses = new Course[]
        {
            new(0, "Dominated", new([5UL]), 2), // Mask 101000
            new(1, "Longest", new([21Ul]), 3),  // Mask 101010
            new(2, "Control", new([18Ul]), 2),  // Mask 010010
            new(3, "Rarest", new([40UL]), 2),   // Mask 000101
        };

        var dataSet = new EventDataSet(6, ImmutableCollectionsMarshal.AsImmutableArray(courses));

        var solver = new BeamSearchSolver(3);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual!.Length.Should().Be(4);
        actual[0].Should().Be(new BeamSearchSolver.CourseResult("Rarest", true));
        actual[1].Should().Be(new BeamSearchSolver.CourseResult("Longest", true));
        actual[2].Should().Be(new BeamSearchSolver.CourseResult("Control", true));
        actual[3].Should().Be(new BeamSearchSolver.CourseResult("Dominated", false));
    }

    [Fact]
    public void TrySolve_ShouldSortByAlphabeticalWhenIdentical()
    {
        // Setup
        var courses = new Course[]
        {
            new(0, "A", new([3UL]), 2),
            new(1, "B", new([3Ul]), 2),
        };

        var dataSet = new EventDataSet(2, ImmutableCollectionsMarshal.AsImmutableArray(courses));

        var solver = new BeamSearchSolver(1);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual!.Length.Should().Be(2);
        actual[0].Should().Be(new BeamSearchSolver.CourseResult("A", true));
        actual[1].Should().Be(new BeamSearchSolver.CourseResult("B", false));
    }
}
