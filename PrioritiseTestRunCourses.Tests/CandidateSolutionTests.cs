using FluentAssertions;
using PrioritiseTestRunCourses.Data;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Xunit;

namespace PrioritiseTestRunCourses.Tests;

public class CandidateSolutionTests
{
    [Fact]
    public void RarityPriorityComparer_ShouldRankLowerRaritySum_AsHigherPriority()
    {
        // SETUP
        var rarityLookup = new Dictionary<string, float>
        {
            { "Rare", 1.0f },
            { "Common", 0.1f }
        }.ToFrozenDictionary();

        var comparer = new CandidateSolution.RarityPriorityComparer(rarityLookup, 1.0F);

        var solutionA = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Rare"]);

        var solutionB = new CandidateSolution(
            ImmutableDictionary<string, int>.Empty,
            ["Common"]);

        // ACT
        var result = comparer.Compare(solutionA, solutionB);

        // ASSERT
        result.Should().BePositive("because solution A has a higher rarity sum than solution B");
    }

    [Fact]
    public void RarityPriorityComparer_WhenRarityIsEqual_ShouldRankFewerCourses_AsHigherPriority()
    {
        // SETUP
        var rarityLookup = new Dictionary<string, float>().ToFrozenDictionary();
        var comparer = new CandidateSolution.RarityPriorityComparer(rarityLookup, 1.0F);

        var solutionA = new CandidateSolution(
            new Dictionary<string, int> { { "C1", 1 }, { "C2", 2 } }.ToImmutableDictionary(),
            ["Control1"]);

        var solutionB = new CandidateSolution(
            new Dictionary<string, int> { { "C1", 1 } }.ToImmutableDictionary(),
            ["Control1"]);

        // ACT
        var result = comparer.Compare(solutionA, solutionB);

        // ASSERT
        result.Should().BePositive("because A uses more courses than B to cover the same rarity");
    }

    [Fact]
    public void Compare_WhenXIsNull_ShouldReturnNegative()
    {
        // SETUP
        var comparer = new CandidateSolution.RarityPriorityComparer(
            FrozenDictionary<string, float>.Empty, 1.0F);

        var solutionY = CandidateSolution.Initial(["Control1"]);

        // ACT
        var result = comparer.Compare(null, solutionY);

        // ASSERT
        result.Should().Be(-1, "because null should be sorted before a non-null instance");
    }

    [Fact]
    public void Compare_WhenYIsNull_ShouldReturnNegative()
    {
        // SETUP
        var comparer = new CandidateSolution.RarityPriorityComparer(
            FrozenDictionary<string, float>.Empty, 1.0F);

        var solutionX = CandidateSolution.Initial(["Control1"]);

        // ACT
        var result = comparer.Compare(solutionX, null);

        // ASSERT
        result.Should().Be(1, "because null should be sorted before a non-null instance");
    }

    [Fact]
    public void Compare_WhenBothIsNull_ShouldReturnZero()
    {
        // SETUP
        var comparer = new CandidateSolution.RarityPriorityComparer(
            FrozenDictionary<string, float>.Empty, 1.0F);

        // ACT
        var result = comparer.Compare(null, null);

        // ASSERT
        result.Should().Be(0, "because null is equvivalent to null");
    }

    [Fact]
    public void Compare_WhenControlIsMissingFromLookup_ShouldUseDefaultRarity()
    {
        // SETUP
        var rarityLookup = new Dictionary<string, float> { { "Common", 0.1f } }.ToFrozenDictionary();
        var comparer = new CandidateSolution.RarityPriorityComparer(rarityLookup, 1.0F);

        var solutionA = CandidateSolution.Initial(["UnknownControl"]);
        var solutionB = CandidateSolution.Initial(["Common"]);

        // ACT
        var result = comparer.Compare(solutionA, solutionB);

        // ASSERT
        result.Should().BePositive("because the unknown control in A defaulted to 1.0, which is heavier than 0.1");
    }
}