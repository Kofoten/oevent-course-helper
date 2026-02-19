using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitMaskTests
{
    [Fact]
    public void IsSubsetOf_ShouldReturnTrue()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([10UL, 10Ul]);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void IsSubsetOf_ShouldReturnFalse()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([5UL, 5Ul]);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_ShouldReturnTrue()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);

        // Act
        var actual = a.IsIdenticalTo(b);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void IsIdenticalTo_ShouldReturnFalse()
    {
        // Setup
        var a = new BitMask([0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL]);
        var b = new BitMask([10UL, 10Ul]);

        // Act
        var actual = a.IsIdenticalTo(b);

        // Assert
        actual.Should().BeFalse();
    }
}
