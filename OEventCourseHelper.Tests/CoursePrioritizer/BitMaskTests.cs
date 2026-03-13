using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitMaskTests
{
    [Fact]
    public void IsSubsetOf_ShouldReturnTrue()
    {
        // Setup
        var a = new BitMask([0b10101010101UL, 0b10101010101UL]);
        var b = new BitMask([0b10000000101UL, 0b00001010000UL]);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void IsSubsetOf_ShouldReturnFalse()
    {
        // Setup
        var a = new BitMask([0b10101010101UL, 0b10101010101UL]);
        var b = new BitMask([0b11000100101UL, 0b00001110000UL]);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);

        // Act
        var actual = a.Equals(b);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([10UL, 10Ul]);

        // Act
        var actual = a.Equals(b);

        // Assert
        actual.Should().BeFalse();
    }
}
