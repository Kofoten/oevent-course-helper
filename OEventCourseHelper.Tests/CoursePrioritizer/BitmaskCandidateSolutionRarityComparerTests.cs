using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitmaskCandidateSolutionRarityComparerTests
{
    private BitmaskCandidateSolution.RarityComparer comparer = new();

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
        var actual = comparer.Compare(null, new BitmaskCandidateSolution([], [], [], 0));

        // Assert
        actual.Should().BeLessThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnPositive_WhenYIsNull()
    {
        // Act
        var actual = comparer.Compare(new BitmaskCandidateSolution([], [], [], 0), null);

        // Assert
        actual.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXHasLowerRarityScore()
    {
        // Setup
        var x = new BitmaskCandidateSolution([], [], [], 0.5F);
        var y = new BitmaskCandidateSolution([], [], [], 1.0F);

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenRarityScoreIsEqualAndXHasFewerCourses()
    {
        // Setup
        var courseA = new CourseMask(new CourseMask.CourseMaskId(0), "A", [], 0);
        var courseB = new CourseMask(new CourseMask.CourseMaskId(1), "B", [], 0);
        var courseC = new CourseMask(new CourseMask.CourseMaskId(2), "C", [], 0);

        var x = new BitmaskCandidateSolution([courseA], [], [], 0.5F);
        var y = new BitmaskCandidateSolution([courseB, courseC], [], [], 0.5F);

        // Act
        var actual = comparer.Compare(x, y);

        // Assert
        actual.Should().BeLessThan(0);
    }
}
