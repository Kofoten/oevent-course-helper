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
        var courseMask = new CourseMask(new CourseMask.CourseMaskId(0), "Dummy", [], 0);

        // Act
        seeker.Process(0, courseMask);
        seeker.Process(1, courseMask);
        seeker.Process(2, courseMask);
        seeker.Process(3, courseMask);

        // Assert
        seeker.IndexOfRarest.Should().Be(1);
    }
}
