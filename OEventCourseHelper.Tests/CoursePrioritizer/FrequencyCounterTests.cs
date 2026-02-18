using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class FrequencyCounterTests
{
    [Fact]
    public void Process_ShouldCountFrequencyOfInputIndex()
    {
        // Setup
        var frequencies = new int[4];
        var counter = new FrequencyCounter(frequencies);
        var courseMask = new CourseMask(new CourseMask.CourseMaskId(0), "Dummy", [], 0);

        // Act
        counter.Process(0, courseMask);
        counter.Process(1, courseMask);
        counter.Process(1, courseMask);
        counter.Process(3, courseMask);

        // Assert
        frequencies[0].Should().Be(1);
        frequencies[1].Should().Be(2);
        frequencies[2].Should().Be(0);
        frequencies[3].Should().Be(1);
    }
}
