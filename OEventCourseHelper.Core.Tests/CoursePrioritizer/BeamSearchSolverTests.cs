using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

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
        var actual = solver.Solve(dataSet);

        // Assert
        actual.Success.Should().BeTrue();
        actual.CourseMask!.Value.PopCount.Should().Be(3);
        actual.PriorityOrder.Length.Should().Be(4);
        actual.PriorityOrder[0].Should().Be(new PrioritizedCourse("Rarest", true));
        actual.PriorityOrder[1].Should().Be(new PrioritizedCourse("Longest", true));
        actual.PriorityOrder[2].Should().Be(new PrioritizedCourse("Control", true));
        actual.PriorityOrder[3].Should().Be(new PrioritizedCourse("Dominated", false));
    }

    [Fact]
    public void TrySolve_ShouldSortByCourseIndexWhenIdentical()
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
        var actual = solver.Solve(dataSet);

        // Assert
        actual.Success.Should().BeTrue();
        actual.PriorityOrder.Length.Should().Be(2);
        actual.PriorityOrder[0].Should().Be(new PrioritizedCourse("B", true));
        actual.PriorityOrder[1].Should().Be(new PrioritizedCourse("A", false));
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
        var actual = solver.Solve(dataSet);

        // Assert
        actual.Success.Should().BeTrue();
        actual.PriorityOrder.Should().ContainEquivalentOf(new PrioritizedCourse("Superset", true));
        actual.PriorityOrder.Should().ContainEquivalentOf(new PrioritizedCourse("Other", true));
        actual.PriorityOrder.Should().ContainEquivalentOf(new PrioritizedCourse("Subset", false));
    }

    [Fact]
    public void TrySolve_ShouldHandleEmptyDataSet()
    {
        // Setup
        var dataSet = new EventDataSet([], []);
        var solver = new BeamSearchSolver(3);

        // Act
        var actual = solver.Solve(dataSet);

        // Assert
        actual.Success.Should().BeTrue();
        actual.PriorityOrder.Should().BeEmpty();
    }
}
