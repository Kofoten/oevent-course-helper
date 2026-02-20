using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CandidateSolutionRarityComparerTests
{
    private readonly CandidateSolution.RarityComparer comparer = new();

    [Fact]
    public void Compare_ShouldReturnZero_WhenBothAreNull()
    {
        // Act
        var actual = comparer.Compare(null, null);

        // Assert
        actual.Should().Be(0);
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXIsNull()
    {
        // Act
        var actual = comparer.Compare(null, new CandidateSolution([], new([]), new([]), 0));

        // Assert
        actual.Should().BeLessThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnPositive_WhenYIsNull()
    {
        // Act
        var actual = comparer.Compare(new CandidateSolution([], new([]), new([]), 0), null);

        // Assert
        actual.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateSolution([], new([]), new([]), 0.5F);
        var y = new CandidateSolution([], new([]), new([]), 1.0F);

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

        var x = new CandidateSolution([courseA], new([]), new([]), 0.5F);
        var y = new CandidateSolution([courseB, courseC], new([]), new([]), 0.5F);

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }
}
