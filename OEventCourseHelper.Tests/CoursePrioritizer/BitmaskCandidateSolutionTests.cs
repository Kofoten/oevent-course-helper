using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
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
            1,
            [],
            controlRarityLookup,
            []);

        // Act
        var actual = BitmaskCandidateSolution.Initial(context);

        // Assert
        actual.CourseOrder.Should().HaveCount(0);
        actual.RarityScore.Should().Be(48.0F);
        actual.IncludedCoursesMask.Buckets.Should().HaveCount(1);
        actual.IncludedCoursesMask.Buckets[0].Should().Be(0);
        actual.UnvisitedControlsMask.Buckets.Should().HaveCount(2);
        actual.UnvisitedControlsMask.Buckets[0].Should().Be(ulong.MaxValue);
        actual.UnvisitedControlsMask.Buckets[1].Should().Be((1UL << 32) - 1);
    }

    [Fact]
    public void AddCourse_ShouldAddCourseNameAndFlipBitsAndReduceRarityScore()
    {
        // Setup
        ImmutableArray<float> controlRarityLookup = [0.5F, 0.2F, 0.25F, 0.67F, 0.1F];
        var course = new CourseMask(new CourseMask.CourseMaskId(0), "A", new([19UL]), 3);
        var context = new BitmaskBeamSearchSolverContext(5, 1.72F, 1, 1, [course], controlRarityLookup, []);
        var solution = new BitmaskCandidateSolution([], new([0UL]), new([31UL]), 1.72F);

        // Act
        var actual = solution.AddCourse(course, context);

        // Assert
        actual.CourseOrder.Should().HaveCount(1);
        actual.CourseOrder[0].Should().Be(course);
        actual.IncludedCoursesMask.Buckets[0].Should().Be(1Ul);
        actual.UnvisitedControlsMask.Buckets.Should().HaveCount(1);
        actual.UnvisitedControlsMask.Buckets[0].Should().Be(12UL);
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

        var course = new CourseMask(new CourseMask.CourseMaskId(0), "A", new([alternating, ((1UL << 32) - 1) & alternating]), 48);
        var context = new BitmaskBeamSearchSolverContext(totalEventControlCount, 48.0F, 2, 1, [course], controlRarityLookup, []);
        var solution = new BitmaskCandidateSolution([], new([0UL]), new([ulong.MaxValue, (1UL << 32) - 1]), 48.0F);

        // Act
        var actualRarityGain = solution.GetPotentialRarityGain(course, controlRarityLookup);
        var actualSolution = solution.AddCourse(course, context);

        // Assert
        actualRarityGain.Should().Be(24.0F);
        actualSolution.RarityScore.Should().Be(24.0F);
    }
}
