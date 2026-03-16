using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CandidateBlueprintTests
{
    [Fact]
    public void Materialize_ShouldReturnParentNoCourseAdded()
    {
        // Setup
        var parent = new CandidateSolution([], new([]), new([]), 0UL);
        var blueprint = new CandidateBlueprint(parent);

        // Act
        var actual = blueprint.Materialize();

        // Assert
        actual.Should().Be(parent);
    }

    [Fact]
    public void Materialize_ShouldReturnNewCandidateSolution()
    {
        // Setup
        var parent = new CandidateSolution([], new([0b0UL]), new([0b1UL]), 1UL);
        var course = new Course(0, "Course", new([0b1UL]), 1);
        var blueprint = new CandidateBlueprint(parent, course, 0UL);

        // Act
        var actual = blueprint.Materialize();

        // Assert
        actual.IsComplete.Should().BeTrue();
        actual.CourseCount.Should().Be(1);
        actual.CourseOrder.Should().HaveCount(1);
        actual.CourseOrder[0].Should().Be(course);
        actual.IncludedCoursesMask.Should().Be(new BitMask([0b1UL]));
        actual.UnvisitedControlsMask.Should().Be(new BitMask([0b0UL]));
        actual.RarityScore.Should().Be(0UL);
    }
}
