using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class BitmaskBeamSearchSolverContextTests
{
    [Fact]
    public void Create_ShouldCreateCorrectlyInitializedInstance()
    {
        // Setup
        var totalControlCount = 6;
        var builders = new CourseMask.Builder[]
        {
            new()
            {
                CourseName = "Course 1",
                ControlMask = [5UL], // 101000
                ControlCount = 2,
            },
            new()
            {
                CourseName = "Course 2",
                ControlMask = [21Ul], // 101010
                ControlCount = 3,
            },
            new()
            {
                CourseName = "Course 3",
                ControlMask = [18Ul], // 010010
                ControlCount = 2,
            },
            new()
            {
                CourseName = "Course 4",
                ControlMask = [40UL], //000101
                ControlCount = 2,
            },
        };

        // Act
        var actual = BitmaskBeamSearchSolverContext.Create(totalControlCount, builders);

        // Assert
        actual.TotalEventControlCount.Should().Be(6);
        actual.TotalControlRaritySum.Should().Be(4.5F);
        actual.ControlMaskBucketCount.Should().Be(1);
        actual.ControlRarityLookup.Should().HaveCount(6);
        actual.ControlRarityLookup.Should().BeEquivalentTo(
            [0.5F, 1.0F, 0.5F, 1.0F, 0.5F, 1.0F],
            o => o.WithStrictOrdering());
        actual.CourseMasks.Should().HaveCount(4);
        actual.CourseMasks.Should().BeEquivalentTo([
            new CourseMask(new CourseMask.CourseMaskId(0), "Course 1", new([5UL]), 2),
            new CourseMask(new CourseMask.CourseMaskId(1), "Course 2", new([21Ul]), 3),
            new CourseMask(new CourseMask.CourseMaskId(2), "Course 3", new([18Ul]), 2),
            new CourseMask(new CourseMask.CourseMaskId(3), "Course 4", new([40UL]), 2),
        ], o => o.WithoutStrictOrdering());
    }
}
