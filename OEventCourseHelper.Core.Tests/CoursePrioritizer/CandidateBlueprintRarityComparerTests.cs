using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CandidateBlueprintRarityComparerTests
{
    private readonly CandidateBlueprint.RarityComparer rarityComparer = new();

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new([], new([]), new([]), 2UL));

        // Act
        var actual = rarityComparer.Compare(x, y);

        // Assert
        actual.Should().BeNegative();
    }

    [Fact]
    public void Compare_ShouldReturnPositive_WhenYHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([]), new([]), 2UL));
        var y = new CandidateBlueprint(new([], new([]), new([]), 1UL));

        // Act
        var actual = rarityComparer.Compare(x, y);

        // Assert
        actual.Should().BePositive();
    }

    [Fact]
    public void Compare_ShouldReturnNegative_WhenXProjectedIncludedCourseMaskIsLower()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([0b010]), new([]), 1UL), new(0, "Course 1", new([]), 1), 1UL);
        var y = new CandidateBlueprint(new([], new([0b101]), new([]), 1UL), new(1, "Course 2", new([]), 1), 1UL);

        // Act
        var actual = rarityComparer.Compare(x, y);

        // Assert
        actual.Should().BeNegative();
    }

    [Fact]
    public void Compare_ShouldReturnPositive_WhenYProjectedIncludedCourseMaskIsLower()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([0b110]), new([]), 1UL), new(0, "Course 1", new([]), 1), 1UL);
        var y = new CandidateBlueprint(new([], new([0b001]), new([]), 1UL), new(1, "Course 2", new([]), 1), 1UL);

        // Act
        var actual = rarityComparer.Compare(x, y);

        // Assert
        actual.Should().BePositive();
    }

    [Fact]
    public void Compare_ShouldReturnZero_WhenProjectedIncludedCourseMaskIsEqual()
    {
        // Setup
        var x = new CandidateBlueprint(new([], new([0b110]), new([]), 1UL), new(0, "Course 1", new([]), 1), 1UL);
        var y = new CandidateBlueprint(new([], new([0b101]), new([]), 1UL), new(1, "Course 2", new([]), 1), 1UL);

        // Act
        var actual = rarityComparer.Compare(x, y);

        // Assert
        actual.Should().Be(0);
    }
}
