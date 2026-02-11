using FluentAssertions;
using OEventCourseHelper.Extensions;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests;

public class ImmutableHashSetExtensionsTests
{
    [Fact]
    public void CalculatePotentialRarityGain_And_RemoveCourses_ShouldReturnSameRarityGain()
    {
        // SETUP
        var defaultRarity = 1.0F;

        var controlRarityLookup = new Dictionary<string, float>()
        {
            { "A", 0.8F },
            { "B", 0.67F },
            { "C", 0.5F },
        }.ToFrozenDictionary();

        ImmutableHashSet<string> sourceSet = ["A", "B", "C", "D"];
        IEnumerable<string> courseControls = ["A", "C", "D"];

        // ACT
        var potentialRarityGain = sourceSet.CalculatePotentialRarityGain(
            courseControls,
            controlRarityLookup,
            defaultRarity);

        _ = sourceSet.RemoveControls(
            courseControls,
            controlRarityLookup,
            defaultRarity,
            out var actualRarityGain);

        // ASSERT
        var expextedRarityGain = 2.3F;

        potentialRarityGain.Should().Be(expextedRarityGain);
        actualRarityGain.Should().Be(expextedRarityGain);
    }
}
