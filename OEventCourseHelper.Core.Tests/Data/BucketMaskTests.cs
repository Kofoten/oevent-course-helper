using FluentAssertions;
using OEventCourseHelper.Core.Data;

namespace OEventCourseHelper.Core.Tests.Data;

public class BucketMaskTests
{
    [Fact]
    public void FromBitIndex_ShouldCreateCorrectBucketMask()
    {
        // Act
        var actual = BitMask.BucketMask.FromBitIndex(64);

        // Assert
        actual.BucketIndex.Should().Be(1);
        actual.BucketValue.Should().Be(0b1UL);
    }
}
