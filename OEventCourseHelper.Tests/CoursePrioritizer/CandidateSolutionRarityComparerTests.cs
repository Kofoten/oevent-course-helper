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
        var x = new CandidateBlueprint(new([], new([]), new([]), 0.5F));
        var y = new CandidateBlueprint(new([], new([]), new([]), 1.0F));

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenRarityScoreIsEqualAndXHasFewerCourses()
    {
        // Setup
        var courseA = new Course(0, "A", new([]), 0);
        var courseB = new Course(1, "B", new([]), 0);
        var courseC = new Course(2, "C", new([]), 0);

        var x = new CandidateBlueprint(new([courseA], new([]), new([]), 0.5F));
        var y = new CandidateBlueprint(new([courseB, courseC], new([]), new([]), 0.5F));

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }
}
