using FluentAssertions;
using PrioritiseTestRunCourses.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Xunit;

namespace PrioritiseTestRunCourses.Tests;

public class CandidateSolutionTests
{
    private readonly CandidateSolution.RarityPriorityComparer comparer = new();

    [Fact]
    public void RarityPriorityComparer_ShouldRankLowerRaritySum_AsHigherPriority()
    {
        // SETUP
        var solutionA = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Rare"],
            1.0F);

        var solutionB = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Common"],
            0.1F);

        // ACT
        var result = comparer.Compare(solutionA, solutionB);

        // ASSERT
        result.Should().BePositive("because solution A has a higher rarity sum than solution B");
    }

    [Fact]
    public void RarityPriorityComparer_WhenRarityIsEqual_ShouldRankFewerCourses_AsHigherPriority()
    {
        // SETUP
        var solutionA = new CandidateSolution(
            new Dictionary<string, int> { { "C1", 1 }, { "C2", 2 } }.ToImmutableDictionary(),
            ["Control1"],
            1.0F);

        var solutionB = new CandidateSolution(
            new Dictionary<string, int> { { "C1", 1 } }.ToImmutableDictionary(),
            ["Control1"],
            1.0F);

        // ACT
        var result = comparer.Compare(solutionA, solutionB);

        // ASSERT
        result.Should().BePositive("because A uses more courses than B to cover the same rarity");
    }

    [Fact]
    public void Compare_WhenXIsNull_ShouldReturnNegative()
    {
        // SETUP
        var solutionY = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Control1"]
            , 1.0F);

        // ACT
        var result = comparer.Compare(null, solutionY);

        // ASSERT
        result.Should().Be(-1, "because null should be sorted before a non-null instance");
    }

    [Fact]
    public void Compare_WhenYIsNull_ShouldReturnNegative()
    {
        // SETUP
        var solutionX = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Control1"]
            , 1.0F);

        // ACT
        var result = comparer.Compare(solutionX, null);

        // ASSERT
        result.Should().Be(1, "because null should be sorted before a non-null instance");
    }

    [Fact]
    public void Compare_WhenBothIsNull_ShouldReturnZero()
    {
        // ACT
        var result = comparer.Compare(null, null);

        // ASSERT
        result.Should().Be(0, "because null is equvivalent to null");
    }
}
