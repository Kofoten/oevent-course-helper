using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitMaskWorkspaceTests
{
    [Fact]
    public void AndBucketAt_ShouldMutateBucketAtIndex()
    {
        // Setup
        var initialValue = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var other = new BitMask([0b1UL, 0b1UL]);
        var workspace = new BitMask.Workspace(initialValue);

        // Act
        workspace.AndBucketAt(1, other);

        // Assert
        var setBits = new List<int>();
        foreach (var index in workspace)
        {
            setBits.Add(index);
        }

        setBits.Should().HaveCount(65);
        setBits.Should().BeEquivalentTo(Enumerable.Range(0, 65), conf => conf.WithStrictOrdering());
    }

    [Fact]
    public void AndBucketAt_ShouldThrowInvalidOperationException()
    {
        // Setup
        var other = new BitMask([0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.AndBucketAt(1, other);
        };

        // Act and Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AndBucketAt_ShouldThrowIndexOutOfRangeException()
    {
        // Setup
        var other = new BitMask([0b1UL, 0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.AndBucketAt(4, other);
        };

        // Act and Assert
        action.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void AndNotBucketAt_ShouldMutateBucketAtIndex()
    {
        // Setup
        var initialValue = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var other = new BitMask([0b1UL, ~0b1UL]);
        var workspace = new BitMask.Workspace(initialValue);

        // Act
        workspace.AndNotBucketAt(1, other);

        // Assert
        var setBits = new List<int>();
        foreach (var index in workspace)
        {
            setBits.Add(index);
        }

        setBits.Should().HaveCount(65);
        setBits.Should().BeEquivalentTo(Enumerable.Range(0, 65), conf => conf.WithStrictOrdering());
    }

    [Fact]
    public void AndNotBucketAt_ShouldThrowInvalidOperationException()
    {
        // Setup
        var other = new BitMask([0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.AndNotBucketAt(1, other);
        };

        // Act and Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AndNotBucketAt_ShouldThrowIndexOutOfRangeException()
    {
        // Setup
        var other = new BitMask([0b1UL, 0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.AndNotBucketAt(4, other);
        };

        // Act and Assert
        action.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void OrBucketAt_ShouldMutateBucketAtIndex()
    {
        // Setup
        var initialValue = new BitMask([ulong.MaxValue, 0b0UL]);
        var other = new BitMask([0b1UL, 0b1UL]);
        var workspace = new BitMask.Workspace(initialValue);

        // Act
        workspace.OrBucketAt(1, other);

        // Assert
        var setBits = new List<int>();
        foreach (var index in workspace)
        {
            setBits.Add(index);
        }

        setBits.Should().HaveCount(65);
        setBits.Should().BeEquivalentTo(Enumerable.Range(0, 65), conf => conf.WithStrictOrdering());
    }

    [Fact]
    public void OrBucketAt_ShouldThrowInvalidOperationException()
    {
        // Setup
        var other = new BitMask([0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.OrBucketAt(1, other);
        };

        // Act and Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OrBucketAt_ShouldThrowIndexOutOfRangeException()
    {
        // Setup
        var other = new BitMask([0b1UL, 0b1UL]);
        var action = () =>
        {
            var workspace = new BitMask.Workspace(2);
            workspace.OrBucketAt(4, other);
        };

        // Act and Assert
        action.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Clear_ShouldSetAllBitsToZero()
    {
        // Setup
        var initialValue = new BitMask([ulong.MaxValue, ulong.MaxValue]);
        var workspace = new BitMask.Workspace(initialValue);

        // Act
        workspace.Clear();

        // Assert
        var setBits = new List<int>();
        foreach (var index in workspace)
        {
            setBits.Add(index);
        }

        setBits.Should().HaveCount(0);
    }
}
