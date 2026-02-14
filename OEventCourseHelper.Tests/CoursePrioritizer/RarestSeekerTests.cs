using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class RarestSeekerTests
{
    [Fact]
    public void Process_ShouldFindRarestInputIndex()
    {
        // Setup
        ImmutableArray<float> controlRarityLookup = [0.1F, 0.91F, 0.3F, 0.5F];
        var seeker = new RarestSeeker(controlRarityLookup);

        // Act
        seeker.Process(0);
        seeker.Process(1);
        seeker.Process(2);
        seeker.Process(3);

        // Assert
        seeker.IndexOfRarest.Should().Be(1);
    }
}
