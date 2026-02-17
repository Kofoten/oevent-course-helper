using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitmaskCandidateSolutionTests
{
    [Fact]
    public void Initial_ShouldCreateNewBitmaskCandidateSolution()
    {
        // Setup
        var totalEventControlCount = 96;
        var controlRarityLookup = Enumerable.Range(0, totalEventControlCount)
            .Select(_ => 0.5F)
            .ToImmutableArray();

        var context = new BitmaskBeamSearchSolverContext(
            totalEventControlCount,
            controlRarityLookup.Sum(),
            2,
            FrozenSet<CourseMask>.Empty,
            controlRarityLookup);

        // Act
        var actual = BitmaskCandidateSolution.Initial(context);

        // Assert
        actual.Courses.Should().HaveCount(0);
        actual.RarityScore.Should().Be(48.0F);
        actual.UnvisitedControlsMask.Should().HaveCount(2);
        actual.UnvisitedControlsMask[0].Should().Be(ulong.MaxValue);
        actual.UnvisitedControlsMask[1].Should().Be((1UL << 32) - 1);
    }

    [Fact]
    public void AddCourse_ShouldAddCourseNameAndFlipBitsAndReduceRarityScore()
    {
        // Setup
        ImmutableArray<float> controlRarityLookup = [0.5F, 0.2F, 0.25F, 0.67F, 0.1F];
        var solution = new BitmaskCandidateSolution([], [31UL], 1.72F);
        var course = new CourseMask("A", [19UL], 3);

        // Act
        var actual = solution.AddCourse(course, controlRarityLookup);

        // Assert
        actual.Courses.Should().HaveCount(1);
        actual.Courses[0].Should().Be("A");
        actual.UnvisitedControlsMask.Should().HaveCount(1);
        actual.UnvisitedControlsMask[0].Should().Be(12UL);
        actual.RarityScore.Should().Be(0.92F);
    }

    [Fact]
    public void GetPotentialRarityGain_ShouldReturnSameAsReductionByAddCourse()
    {
        // Setup
        var alternating = 0xAAAAAAAAAAAAAAAAUL;
        var totalEventControlCount = 96;
        var controlRarityLookup = Enumerable.Range(0, totalEventControlCount)
            .Select(_ => 0.5F)
            .ToImmutableArray();

        var solution = new BitmaskCandidateSolution([], [ulong.MaxValue, (1UL << 32) - 1], 48.0F);
        var course = new CourseMask("A", [alternating, ((1UL << 32) - 1) & alternating], 48);

        // Act
        var actualRarityGain = solution.GetPotentialRarityGain(course, controlRarityLookup);
        var actualSolution = solution.AddCourse(course, controlRarityLookup);

        // Assert
        actualRarityGain.Should().Be(24.0F);
        actualSolution.RarityScore.Should().Be(24.0F);
    }
}
