using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CandidateSolutionRarityComparerTests
{
    private readonly CandidateBlueprint.RarityComparer comparer = new();

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([]), new([]), 5000000UL));
        var y = new CandidateBlueprint(new([], new([]), new([]), 10000000UL));

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }

    [Fact] // This test is broken
    public void Compare_ShouldReturnNegative_WhenRarityScoreIsEqualAndXHasFewerCourses()
    {
        // Setup
        var courseA = new Course(0, "A", new([]), 0);
        var courseB = new Course(1, "B", new([]), 0);
        var courseC = new Course(2, "C", new([]), 0);

        var x = new CandidateBlueprint(new([courseA], new([]), new([]), 5000000UL));
        var y = new CandidateBlueprint(new([courseB, courseC], new([]), new([]), 5000000UL));

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }
}
