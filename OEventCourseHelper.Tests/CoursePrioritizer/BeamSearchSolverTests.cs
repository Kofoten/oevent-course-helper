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
            new(0, "Dominated", new([0b000101UL]), 2),
            new(1, "Longest", new([0b010101UL]), 3),
            new(2, "Control", new([0b010010UL]), 2),
            new(3, "Rarest", new([0b101000UL]), 2),
        };

        var dataSet = new EventDataSet(["31", "32", "33", "34", "35", "36"], ImmutableCollectionsMarshal.AsImmutableArray(courses));

        var solver = new BeamSearchSolver(3);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual!.Length.Should().Be(4);
        actual[0].Should().Be(new BeamSearchSolver.ResultItem("Rarest", true));
        actual[1].Should().Be(new BeamSearchSolver.ResultItem("Longest", true));
        actual[2].Should().Be(new BeamSearchSolver.ResultItem("Control", true));
        actual[3].Should().Be(new BeamSearchSolver.ResultItem("Dominated", false));
    }

    [Fact]
    public void TrySolve_ShouldSortByAlphabeticalWhenIdentical()
    {
        // Setup
        var courses = new Course[]
        {
            new(0, "B", new([0b11UL]), 2),
            new(1, "A", new([0b11UL]), 2),
        };

        var dataSet = new EventDataSet(["31", "32"], ImmutableCollectionsMarshal.AsImmutableArray(courses));
        var solver = new BeamSearchSolver(1);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual!.Length.Should().Be(2);
        actual[0].Should().Be(new BeamSearchSolver.ResultItem("B", true));
        actual[1].Should().Be(new BeamSearchSolver.ResultItem("A", false));
    }

    [Fact]
    public void TrySolve_ShouldReturnFalse_WhenNotAllControlsCanBeVisited()
    {
        // Setup
        var courses = new Course[]
        {
            new(0, "IncompleteCourse", new([0b01UL]), 1)
        };

        var dataSet = new EventDataSet(["31", "32"], ImmutableCollectionsMarshal.AsImmutableArray(courses));
        var solver = new BeamSearchSolver(3);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeFalse();
        actual.Should().BeNull();
    }

    [Fact]
    public void TrySolve_ShouldProperlyIdentifyDominatedCourses()
    {
        // Setup
        var courses = new Course[]
        {
            new(0, "Subset", new([0b001UL]), 1),   // Covers "31"
            new(1, "Superset", new([0b011UL]), 2), // Covers "31", "32"
            new(2, "Other", new([0b100UL]), 1)     // Covers "33"
        };

        var dataSet = new EventDataSet(["31", "32", "33"], ImmutableCollectionsMarshal.AsImmutableArray(courses));
        var solver = new BeamSearchSolver(3);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual.Should().NotBeNull();
        actual.Should().ContainEquivalentOf(new BeamSearchSolver.ResultItem("Superset", true));
        actual.Should().ContainEquivalentOf(new BeamSearchSolver.ResultItem("Other", true));
        actual.Should().ContainEquivalentOf(new BeamSearchSolver.ResultItem("Subset", false));
    }

    [Fact]
    public void TrySolve_ShouldHandleEmptyDataSet()
    {
        // Setup
        var dataSet = new EventDataSet([], []);
        var solver = new BeamSearchSolver(3);

        // Act
        var solutionFound = solver.TrySolve(dataSet, out var actual);

        // Assert
        solutionFound.Should().BeTrue();
        actual.Should().BeEmpty();
    }
}
