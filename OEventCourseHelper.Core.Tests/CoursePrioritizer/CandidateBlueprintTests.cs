using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CandidateBlueprintTests
{
    [Fact]
    public void Materialize_ShouldReturnNewCandidateSolution()
    {
        // Setup
        var courseSlice = new BitMask([0b1UL]);
        var parent = new CandidateSolution([], new([0b0UL]), new([0b1UL]), 1UL);
        var course = new Course(0, 0, 1);
        var blueprint = new CandidateBlueprint(parent, course, 0UL);

        // Act
        var actual = blueprint.Materialize(courseSlice, 1);

        // Assert
        actual.IsComplete.Should().BeTrue();
        actual.CourseCount.Should().Be(1);
        actual.CourseOrder.Should().HaveCount(1);
        actual.CourseOrder[0].Should().Be(course);
        actual.IncludedCoursesMask.Should().Be(new Core.Data.BitMask([0b1UL]));
        actual.UnvisitedControlsMask.Should().Be(new BitMask([0b0UL]));
        actual.RarityScore.Should().Be(0UL);
    }
}
