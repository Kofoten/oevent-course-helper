using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class BeamSearchSolverTests
{
    [Fact]
    public void TrySolve_ShouldOrderCoursesCorrectly()
    {
        // Setup
        var courseMask = new BitMask([0b000101UL, 0b010010UL, 0b010101UL, 0b101000UL]);
        var courseNames = ImmutableArray.Create(["Dominated", "Longest", "Control", "Rarest"]);
        var controlNames = ImmutableArray.Create(["31", "32", "33", "34", "35", "36"]);

        var courses = new Course[]
        {
            new(0, 0, 2),
            new(1, 2, 3),
            new(2, 1, 2),
            new(3, 3, 2),
        };

        var dataSet = new EventDataSet(
            controlNames,
            courseNames,
            ImmutableCollectionsMarshal.AsImmutableArray(courses),
            courseMask,
            1);

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
        var courseMask = new BitMask([0b11UL, 0b11UL]);
        var courseNames = ImmutableArray.Create(["B", "A"]);
        var controlNames = ImmutableArray.Create(["31", "32"]);

        var courses = new Course[]
        {
            new(0, 0, 2),
            new(1, 1, 2),
        };

        var dataSet = new EventDataSet(
            controlNames,
            courseNames,
            ImmutableCollectionsMarshal.AsImmutableArray(courses),
            courseMask,
            1);

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
        var courseMask = new BitMask([0b001UL, 0b011UL, 0b100UL]);
        var courseNames = ImmutableArray.Create(["Subset", "Superset", "Other"]);
        var controlNames = ImmutableArray.Create(["31", "32", "33"]);

        var courses = new Course[]
        {
            new(0, 0, 1), // Covers "31"
            new(1, 1, 2), // Covers "31", "32"
            new(2, 2, 1), // Covers "33"
        };

        var dataSet = new EventDataSet(
            controlNames,
            courseNames,
            ImmutableCollectionsMarshal.AsImmutableArray(courses),
            courseMask,
            1);

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
        var dataSet = new EventDataSet([], [], [], new BitMask([]), 0);
        var solver = new BeamSearchSolver(3);

        // Act
        var actual = solver.Solve(dataSet);

        // Assert
        actual.Success.Should().BeFalse();
    }
}
