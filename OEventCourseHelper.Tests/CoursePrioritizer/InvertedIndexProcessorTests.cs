using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class InvertedIndexProcessorTests
{
    [Fact]
    public void Process_ShouldBuildCorrectInvertedIndex()
    {
        // Setup
        var actual = new ulong[2][];
        var processor = new InvertedIndexProcessor(actual, 1);
        var courseMask = new CourseMask(new CourseMask.CourseMaskId(0), "A", [3UL], 2);

        // Act
        processor.Process(0, courseMask);
        processor.Process(1, courseMask);

        // Assert
        actual[0].Should().NotBeNull();
        actual[0].Should().HaveCount(1);
        actual[0][0].Should().Be(1UL);
        actual[1].Should().NotBeNull();
        actual[1].Should().HaveCount(1);
        actual[1][0].Should().Be(1UL);
    }
}
