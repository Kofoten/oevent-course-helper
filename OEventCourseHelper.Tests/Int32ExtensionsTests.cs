using FluentAssertions;
using OEventCourseHelper.Extensions;

namespace OEventCourseHelper.Tests;

public class Int32ExtensionsTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(64, 1)]
    [InlineData(65, 2)]
    [InlineData(96, 2)]
    [InlineData(192, 3)]
    public void Get64BitBucketCount_ShouldReturnCorrectBucketCount(int inputBits, int expectedBuckets)
    {
        // Act
        int result = inputBits.Get64BitBucketCount();

        // Assert
        result.Should().Be(expectedBuckets);
    }
}
