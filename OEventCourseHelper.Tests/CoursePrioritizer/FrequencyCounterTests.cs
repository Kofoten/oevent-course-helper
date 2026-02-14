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
        var counter = new FrequencyCounter()
        {
            Counts = frequencies,
        };

        // Act
        counter.Process(0);
        counter.Process(1);
        counter.Process(1);
        counter.Process(3);

        // Assert
        frequencies[0].Should().Be(1);
        frequencies[1].Should().Be(2);
        frequencies[2].Should().Be(0);
        frequencies[3].Should().Be(1);
    }
}
