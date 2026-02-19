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
        actual.IncludedCoursesMask.Should().HaveCount(1);
        actual.IncludedCoursesMask[0].Should().Be(0);
        actual.UnvisitedControlsMask.Should().HaveCount(2);
        actual.UnvisitedControlsMask[0].Should().Be(ulong.MaxValue);
        actual.UnvisitedControlsMask[1].Should().Be((1UL << 32) - 1);
    }

    [Fact]
    public void AddCourse_ShouldAddCourseNameAndFlipBitsAndReduceRarityScore()
    {
        // Setup
        ImmutableArray<float> controlRarityLookup = [0.5F, 0.2F, 0.25F, 0.67F, 0.1F];
        var course = new CourseMask(new CourseMask.CourseMaskId(0), "A", [19UL], 3);
        var context = new BitmaskBeamSearchSolverContext(5, 1.72F, 1, 1, [course], controlRarityLookup, []);
        var solution = new BitmaskCandidateSolution([], [0UL], [31UL], 1.72F);

        // Act
        var actual = solution.AddCourse(course, context);

        // Assert
        actual.CourseOrder.Should().HaveCount(1);
        actual.CourseOrder[0].Should().Be(course);
        actual.IncludedCoursesMask[0].Should().Be(1Ul);
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

        var course = new CourseMask(new CourseMask.CourseMaskId(0), "A", [alternating, ((1UL << 32) - 1) & alternating], 48);
        var context = new BitmaskBeamSearchSolverContext(totalEventControlCount, 48.0F, 2, 1, [course], controlRarityLookup, []);
        var solution = new BitmaskCandidateSolution([], [0UL], [ulong.MaxValue, (1UL << 32) - 1], 48.0F);

        // Act
        var actualRarityGain = solution.GetPotentialRarityGain(course, controlRarityLookup);
        var actualSolution = solution.AddCourse(course, context);

        // Assert
        actualRarityGain.Should().Be(24.0F);
        actualSolution.RarityScore.Should().Be(24.0F);
    }

    [Fact]
    public void ContainsCourseMask_ShouldReturnTrue()
    {
        // Setup
        var course = new CourseMask(new CourseMask.CourseMaskId(0), "A", [1UL], 1);
        var solution = new BitmaskCandidateSolution([course], [1UL], [1UL], 0.0F);

        // Act
        var actual = solution.ContainsCourseMask(course);

        // Assert
        actual.Should().BeTrue();
    }
}
