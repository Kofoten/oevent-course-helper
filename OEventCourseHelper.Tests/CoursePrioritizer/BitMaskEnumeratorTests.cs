using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitMaskEnumeratorTests
{
    [Fact]
    public void MoveNext_ShouldTraverseSetIndicies()
    {
        // Setup
        ulong[] buckets = [
            0b10000000_00000000_00000000_00000001_00000000_00000010_00000000_00000000UL,
            0b10000000_00000000_00000000_00000000_00000000_00000000_00000000_00000001UL,
        ];

        var enumerator = new BitMask.BitMaskEnumerator(buckets);

        // Act
        var setBits = new List<int>();
        while (enumerator.MoveNext())
        {
            setBits.Add(enumerator.Current);
        }

        // Assert
        setBits.Should().HaveCount(5);
        setBits.Should().BeEquivalentTo([17, 32, 63, 64, 127], conf => conf.WithStrictOrdering());
    }

    [Fact]
    public void MoveNext_ShouldSkipEmptyBuckets()
    {
        // Setup
        ulong[] buckets = [0b1UL, 0b0UL, 0b1UL];
        var enumerator = new BitMask.BitMaskEnumerator(buckets);

        // Act
        var setBits = new List<int>();
        while (enumerator.MoveNext())
        {
            setBits.Add(enumerator.Current);
        }

        // Assert
        setBits.Should().HaveCount(2);
        setBits.Should().BeEquivalentTo([0, 128], conf => conf.WithStrictOrdering());
    }

    [Fact]
    public void MoveNext_ShouldReturnFalseImmediatelyForEmptyMask()
    {
        // Setup
        ulong[] buckets = [0b0UL, 0b0UL, 0b0UL];
        var enumerator = new BitMask.BitMaskEnumerator(buckets);

        // Act and Assert
        enumerator.MoveNext().Should().BeFalse();
    }
}
