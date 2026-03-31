using FluentAssertions;
using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.Tests.Data;

public class BitMaskBuilderTests
{
    [Theory]
    [InlineData(0b0UL, 0b0UL, true)]
    [InlineData(0b0UL, 0b1UL, false)]
    [InlineData(0b1UL, 0b0UL, false)]
    [InlineData(ulong.MaxValue, 0b1UL, false)]
    public void IsZero_ShouldReturnExpected(ulong bucket1, ulong bucket2, bool expected)
    {
        // Setup
        var mask = new BitMask([bucket1, bucket2]);
        var builder = BitMask.Builder.From(mask);

        // Act and Assert
        builder.IsZero.Should().Be(expected);
    }

    [Fact]
    public void ToBitMask_ShouldThrowInvalidOperationExceptionForUnknownBucketCount()
    {
        // Setup
        var builder = new BitMask.Builder();
        builder.Set(128);

        // Act and assert
        builder.Invoking(x => x.ToBitMask())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Can not create a bit mask with an unknown bucket count.");
    }

    [Fact]
    public void ToBitMask_ShouldThrowInvalidOperationExceptionForResizedInnerArray()
    {
        // Setup
        var mask = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var builder = BitMask.Builder.From(mask);
        builder.Set(128);

        // Act and assert
        builder.Invoking(x => x.ToBitMask())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("The mask grew to * buckets, which exceeds the expected *.");
    }

    [Fact]
    public void ToBitMaskWithParameter_ShouldPadWithEmptyBuckets()
    {
        // Setup
        var mask = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var builder = BitMask.Builder.From(mask);

        // Act
        var actual = builder.ToBitMask(3);

        // Assert
        actual.Buckets.Should().HaveCount(3);
        actual.Buckets[0].Should().Be(ulong.MaxValue);
        actual.Buckets[1].Should().Be(ulong.MaxValue);
        actual.Buckets[2].Should().Be(0b0UL);
    }

    [Fact]
    public void Set_ShouldSetBit()
    {
        // Setup
        var builder = new BitMask.Builder(2);

        // Act and Assert Returns
        builder.Set(64).Should().BeTrue();
        builder.Set(64).Should().BeFalse();
        var actual = builder.ToBitMask();

        // Assert
        builder.IsConsumed.Should().BeTrue();
        actual.Buckets.Should().HaveCount(2);
        actual.Buckets[0].Should().Be(0b0UL);
        actual.Buckets[1].Should().Be(0b1UL);
    }

    [Fact]
    public void Set_ShouldThrowArgumentOutOfRangeExceptionForNegativeIndex()
    {
        // Setup
        var builder = new BitMask.Builder(2);

        // Act and assert
        builder.Invoking(x => x.Set(-1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Set_ShouldThrowInvalidOperationExceptionForConsumedBuilder()
    {
        // Setup
        var builder = new BitMask.Builder(2);
        _ = builder.ToBitMask();

        // Act and assert
        builder.Invoking(x => x.Set(0))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AndNot_ShouldClearBits()
    {
        // Setup
        var initialValue = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var builder = BitMask.Builder.From(initialValue);
        var other = new BitMask([0b1UL, 0b1UL]);

        // Act
        builder.AndNot(other);
        var actual = builder.ToBitMask();

        // Assert
        builder.IsConsumed.Should().BeTrue();
        actual.Buckets.Should().HaveCount(2);
        actual.Buckets[0].Should().Be(ulong.MaxValue - 1);
        actual.Buckets[1].Should().Be(ulong.MaxValue - 1);
    }

    [Fact]
    public void AndNot_ShouldThrowInvalidOperationExceptionForConsumedBuilder()
    {
        // Setup
        var builder = new BitMask.Builder(2);
        _ = builder.ToBitMask();

        // Act and assert
        builder.Invoking(x => x.AndNot(new BitMask([0b0UL, 0b0UL])))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void From_ShouldInitializeWithExistingMask()
    {
        // Setup
        var mask = new BitMask([ulong.MaxValue, 0b101UL]);

        // Act
        var builder = BitMask.Builder.From(mask);
        var actual = builder.ToBitMask();

        // Assert
        builder.IsConsumed.Should().BeTrue();
        actual.Buckets.Should().BeEquivalentTo(mask.Buckets, conf => conf.WithStrictOrdering());
    }
}
