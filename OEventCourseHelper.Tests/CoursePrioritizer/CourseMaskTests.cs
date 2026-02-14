using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CourseMaskTests
{
    [Fact]
    public void IsSubsetOf_ShouldReturnTrue()
    {
        // Setup
        var a = new CourseMask("A", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);
        var b = new CourseMask("B", [10UL, 10Ul], 4);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void IsSubsetOf_ShouldReturnFalse()
    {
        // Setup
        var a = new CourseMask("A", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);
        var b = new CourseMask("B", [5UL, 5Ul], 4);

        // Act
        var actual = b.IsSubsetOf(a);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_ShouldReturnTrue()
    {
        // Setup
        var a = new CourseMask("A", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);
        var b = new CourseMask("B", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);

        // Act
        var actual = a.IsIdenticalTo(b);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void IsIdenticalTo_ShouldReturnFalse()
    {
        // Setup
        var a = new CourseMask("A", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);
        var b = new CourseMask("B", [10UL, 10Ul], 4);

        // Act
        var actual = a.IsIdenticalTo(b);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void ForEachControl_ShouldCopyCourseMask()
    {
        // Setup
        var actual = new ulong[2];
        var a = new CourseMask("A", [0xAAAAAAAAAAAAAAAAUL, 0xAAAAAAAAAAAAAAAAUL], 64);
        var processor = new TestProcessor()
        {
            Copy = actual,
        };

        // Act
        a.ForEachControl(ref processor);

        // Assert
        actual[0].Should().Be(0xAAAAAAAAAAAAAAAAUL);
        actual[1].Should().Be(0xAAAAAAAAAAAAAAAAUL);
    }

    internal struct TestProcessor : CourseMask.IProcessor
    {
        public ulong[] Copy;

        public readonly void Process(int index)
        {
            var wordIndex = index >> 6;
            var bitIndex = index & 63;

            Copy[wordIndex] |= 1UL << bitIndex;
        }
    }
}
