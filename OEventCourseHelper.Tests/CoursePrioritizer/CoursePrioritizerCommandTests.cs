using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Tests.TestUtilities;
using System.Collections.Immutable;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CoursePrioritizerCommandTests
{
    [Fact]
    public void Execute_ShouldReturnValidationFailed_WhenStrictIsEnabledAndControlsAreSkipped()
    {
        // Setup
        var options = new OEventCourseHelperLoggingOptions();
        var monitor = new TestOptionsMonitor<OEventCourseHelperLoggingOptions>(options);
        var fakeLogger = new FakeLogger<CoursePrioritizerCommand>();
        var command = new CoursePrioritizerCommand(fakeLogger, monitor);

        var controls = ImmutableArray.Create("31", "32", "33");
        var courses = ImmutableArray.Create(new Course(0, "Course 1", new BitMask([0b011UL]), 2));
        var dataSet = new EventDataSet(controls, courses);

        // Act
        var result = command.ValidateDataSet(dataSet, strict: true);

        // Assert
        result.Should().BeFalse();
        fakeLogger.Logs.Should().Contain(l => l.Id.Id == 11002);
    }

    [Fact]
    public void Execute_ShouldWarnSkippedControl_WhenStrictIsDisabledAndControlsAreSkipped()
    {
        // Setup
        var options = new OEventCourseHelperLoggingOptions();
        var monitor = new TestOptionsMonitor<OEventCourseHelperLoggingOptions>(options);
        var fakeLogger = new FakeLogger<CoursePrioritizerCommand>();
        var command = new CoursePrioritizerCommand(fakeLogger, monitor);

        var controls = ImmutableArray.Create("31", "32", "33");
        var courses = ImmutableArray.Create(new Course(0, "Course 1", new BitMask([0b011UL]), 2));
        var dataSet = new EventDataSet(controls, courses);

        // Act
        var result = command.ValidateDataSet(dataSet, strict: false);

        // Assert
        result.Should().BeTrue();
        fakeLogger.Logs.Should().Contain(l => l.Id.Id == 11001);
        fakeLogger.Logs.Should().Contain(l => l.Message.Contains("33"));
    }
}
