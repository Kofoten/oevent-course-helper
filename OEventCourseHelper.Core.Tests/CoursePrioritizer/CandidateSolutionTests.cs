using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;
using OEventCourseHelper.Core.CoursePrioritizer.Solver;
using OEventCourseHelper.Core.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CandidateSolutionTests
{
    [Fact]
    public void Initial_ShouldCreateCorrectlyInitializedCandidateSolution()
    {
        // Setup
        var totalEventControlCount = 96;
        var controlRarityLookup = Enumerable.Range(0, totalEventControlCount)
            .Select(_ => 5000000UL)
            .ToImmutableArray();

        var context = new BeamSearchSolverContext(
            BitMask.Fill(totalEventControlCount),
            controlRarityLookup.Aggregate(0UL, (acc, x) => acc + x),
            2,
            1,
            [new Course(0, "The Course", new([0b0UL, 0b1UL]), 1)],
            controlRarityLookup,
            new([]),
            new([]));

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
    public void GetPotentialRarityGain_ShouldReturnCorrectRarityGain()
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
        var actual = solution.GetPotentialRarityGain(course, controlRarityLookup);

        // Assert
        actual.Should().Be(240000000UL);
    }
}
