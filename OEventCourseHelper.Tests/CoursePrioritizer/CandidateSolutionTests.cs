using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CandidateSolutionTests
{
    [Fact]
    public void Initial_ShouldCreateNewBitmaskCandidateSolution()
    {
        // Setup
        var totalEventControlCount = 96;
        var controlRarityLookup = Enumerable.Range(0, totalEventControlCount)
            .Select(_ => 5000000UL)
            .ToImmutableArray();

        var context = new BeamSearchSolverContext(
            totalEventControlCount,
            controlRarityLookup.Aggregate(0UL, (acc, x) => acc + x),
            2,
            1,
            [],
            controlRarityLookup,
            new([]),
            []);

        // Act
        var actual = CandidateSolution.Initial(context);

        // Assert
        actual.CourseOrder.Should().HaveCount(0);
        actual.RarityScore.Should().Be(480000000UL);
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
        ImmutableArray<ulong> controlRarityLookup = [5000000UL, 2000000UL, 2500000UL, 6700000UL, 1000000UL];
        var course = new Course(0, "A", new([19UL]), 3);
        var context = new BeamSearchSolverContext(5, 17200000UL, 1, 1, [course], controlRarityLookup, new([]), []);
        var solution = new CandidateSolution([], new([0UL]), new([31UL]), 17200000UL);

        // Act
        var actual = new CandidateBlueprint(solution, course, 8000000UL);

        // Assert
        //actual.CourseOrder.Should().HaveCount(1);
        //actual.CourseOrder[0].Should().Be(course);
        //actual.IncludedCoursesMask.Buckets[0].Should().Be(1Ul);
        //actual.UnvisitedControlsMask.Buckets.Should().HaveCount(1);
        //actual.UnvisitedControlsMask.Buckets[0].Should().Be(12UL);
        actual.CourseCount.Should().Be(1);
        actual.RarityScore.Should().Be(8000000UL);
    }

    [Fact]
    public void GetPotentialRarityGain_ShouldReturnSameAsReductionByAddCourse()
    {
        // Setup
        var alternating = 0xAAAAAAAAAAAAAAAAUL;
        var totalEventControlCount = 96;
        var controlRarityLookup = Enumerable.Range(0, totalEventControlCount)
            .Select(_ => 5000000UL)
            .ToImmutableArray();

        var course = new Course(0, "A", new([alternating, ((1UL << 32) - 1) & alternating]), 48);
        var solution = new CandidateSolution([], new([0UL]), new([ulong.MaxValue, (1UL << 32) - 1]), 480000000UL);

        // Act
        var actualRarityGain = solution.GetPotentialRarityGain(course, controlRarityLookup);

        // Assert
        actualRarityGain.Should().Be(240000000UL);
    }
}
