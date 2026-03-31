using FluentAssertions;
using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.Tests.Data;

public class BitMaskTests
{
    [Theory]
    [InlineData(0b0UL, 0b0UL, true)]
    [InlineData(0b0UL, 0b0000000000000000000000000000000000000001UL, false)]
    [InlineData(ulong.MaxValue, ulong.MaxValue, false)]
    public void IsZero_ShouldReturnExpected(ulong bucket1, ulong bucket2, bool expected)
    {
        // Setup
        var mask = new BitMask([bucket1, bucket2]);

        // Assert
        mask.IsZero.Should().Be(expected);
    }

    [Theory]
    [InlineData(0b10UL, 0b10UL, 0, false)]
    [InlineData(0b10UL, 0b10UL, 1, true)]
    [InlineData(0b10UL, 0b10UL, 64, false)]
    [InlineData(0b10UL, 0b10UL, 65, true)]
    public void BitAtIndex_ShouldReturnExpected(ulong bucket1, ulong bucket2, int index, bool expected)
    {
        // Setup
        var mask = new BitMask([bucket1, bucket2]);

        // Assert
        mask[index].Should().Be(expected);
    }

    [Fact]
    public void GetEnumerator_ShouldReturnNewEnumerator()
    {
        // Setup
        var mask = new BitMask([0b0UL, 0b1UL]);

        // Act
        var enumerator = mask.GetEnumerator();
        enumerator.MoveNext();

        // Assert
        enumerator.Current.Should().Be(64);
    }

    [Fact]
    public void ImplicitOperator_ConvertsToReadOnlySpan()
    {
        // Setup
        var mask = new BitMask([0b1UL, 0b1UL]);

        // Act
        ReadOnlySpan<ulong> span = mask;

        // Assert
        span.Length.Should().Be(2);
        span.SequenceEqual([0b1UL, 0b1UL]).Should().BeTrue();
    }

    [Fact]
    public void Set_ShouldReturnNewBitMask()
    {
        // Setup
        var mask = new BitMask([0b0UL, 0b0UL]);

        // Act
        var actual = mask.Set(65);

        // Assert
        actual.Buckets[1].Should().Be(0b10UL);
    }

    [Fact]
    public void Set_ShouldThrowIndexOutOfRangeException()
    {
        // Setup
        var mask = new BitMask([0b0UL, 0b0UL]);

        // Act and Assert
        mask.Invoking(x => x.Set(512))
            .Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void AndNot_ShouldReturnNewBitMask()
    {
        // Setup
        var mask = new BitMask([0b1010UL, 0b1010UL]);
        var other = new BitMask([0b1100UL, 0b1100UL]);

        // Act
        var actual = mask.AndNot(other);

        // Assert
        actual.Buckets[0].Should().Be(0b0010UL);
        actual.Buckets[1].Should().Be(0b0010UL);
    }

    [Fact]
    public void AndNot_ShouldThrowInvalidOperationException()
    {
        // Setup
        var mask = new BitMask([0b1010UL, 0b1010UL]);
        var other = new BitMask([0b1100UL]);

        // Act and Assert
        mask.Invoking(x => x.AndNot(other))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0b10000000101UL, 0b00001010000UL, 0b10101010101UL, 0b10101010101UL, true)]
    [InlineData(0b11000100101UL, 0b00001110000UL, 0b10101010101UL, 0b10101010101UL, false)]
    public void IsSubsetOf_ShouldReturnExpected(ulong a1, ulong a2, ulong b1, ulong b2, bool expected)
    {
        // Setup
        var a = new BitMask([a1, a2]);
        var b = new BitMask([b1, b2]);

        // Act
        var actual = a.IsSubsetOf(b);

        // Assert
        actual.Should().Be(expected);
    }


    [Fact]
    public void IsSubsetOf_ShouldThrowInvalidOperationException()
    {
        // Setup
        var mask = new BitMask([0b1010UL, 0b1010UL]);
        var other = new BitMask([0b1100UL]);

        // Act and Assert
        mask.Invoking(x => x.IsSubsetOf(other))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, 1)]
    [InlineData(64, 1)]
    [InlineData(65, 2)]
    [InlineData(200, 4)]
    [InlineData(512, 8)]
    public void GetBucketCount_ShouldReturnCorrectBucketCount(int bitCount, int expected)
    {
        // Act
        var actual = BitMask.GetBucketCount(bitCount);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Fill_ShouldCreateBitMaskWithSetBits()
    {
        // Act
        var mask = BitMask.Fill(65);

        // Assert
        mask.Buckets.Should().HaveCount(2);
        mask.Buckets[0].Should().Be(ulong.MaxValue);
        mask.Buckets[1].Should().Be(0b1UL);
    }

    [Fact]
    public void Zero_ShouldCreateBitMaskWithoutSetBits()
    {
        // Act
        var mask = BitMask.Zero(65);

        // Assert
        mask.Buckets.Should().HaveCount(2);
        mask.Buckets[0].Should().Be(0b0UL);
        mask.Buckets[1].Should().Be(0b0UL);
    }

    [Theory]
    [InlineData(0b10101010UL, 0b10101010UL, 0b10101010UL, 0b10101010UL, true)]
    [InlineData(0b10101010UL, 0b10101010UL, 0b01010101UL, 0b01010101UL, false)]
    public void Equals_ShouldReturnExpected(ulong a1, ulong a2, ulong b1, ulong b2, bool expected)
    {
        // Setup
        var a = new BitMask([a1, a2]);
        var b = new BitMask([b1, b2]);

        // Act
        var actual = a.Equals(b);

        // Assert
        actual.Should().Be(expected);
    }
}
