using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CandidateBlueprintTieBreakComparerTests
{
    private readonly Course defaultCourse = new(0, "Default", new([]), 0);
    private readonly CandidateBlueprint.TieBreakComparer tieBreakComparer = new();

    [Fact]
    private void Compare_ShouldReturnNegative_WhenXHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 2UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BeNegative();
    }

    [Fact]
    private void Compare_ShouldReturnPositive_WhenYHasLowerRarityScore()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 2UL));
        var y = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BePositive();
    }

    [Fact]
    private void Compare_ShouldReturnNegative_WhenXIsEmptyAndYIsNotEmpty()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BeNegative();
    }

    [Fact]
    private void Compare_ShouldReturnPositive_WhenYIsEmptyAndXIsNotEmpty()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BePositive();
    }

    [Fact]
    private void Compare_ShouldReturnZero_WhenBothAreEmpty()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().Be(0);
    }

    [Fact]
    private void Compare_ShouldReturnNegative_WhenFirstInXHasLowerCourseIndex()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([defaultCourse with { CourseIndex = 1 }], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BeNegative();
    }

    [Fact]
    private void Compare_ShouldReturnPositive_WhenFirstInYHasLowerCourseIndex()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([defaultCourse with { CourseIndex = 1 }], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().BePositive();
    }

    [Fact]
    private void Compare_ShouldReturnZero_WhenBothHaveSameFirstCourseIndex()
    {
        // Setup
        var x = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));
        var y = new CandidateBlueprint(new CandidateSolution([defaultCourse], new([]), new([]), 1UL));

        // Act
        var actual = tieBreakComparer.Compare(x, y);

        // Assert
        actual.Should().Be(0);
    }
}
