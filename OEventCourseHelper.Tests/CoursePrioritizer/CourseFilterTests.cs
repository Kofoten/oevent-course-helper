using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Commands.CoursePrioritizer.IO;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CourseFilterTests
{
    [Fact]
    public void Matches_SouldMatchCourse()
    {
        // Setup
        var builder = new Course.Builder()
        {
            CourseName = "Course",
            ControlMask = [1Ul],
            ControlCount = 1,
        };

        var filter = new CourseBuilderFilter(true, ["Course"]);

        // Act
        var actual = filter.Matches(builder);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldNotMatchEmptyCourses()
    {
        // Setup
        var builder = new Course.Builder()
        {
            CourseName = "Empty",
            ControlMask = [0Ul],
            ControlCount = 0,
        };

        var filter = new CourseBuilderFilter(true, []);

        // Act
        var actual = filter.Matches(builder);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldNotMatchCourseNotContainingAnyFilterString()
    {
        // Setup
        var builder = new Course.Builder()
        {
            CourseName = "NoMatch",
            ControlMask = [4Ul],
            ControlCount = 1,
        };

        var filter = new CourseBuilderFilter(false, ["Course"]);

        // Act
        var actual = filter.Matches(builder);

        // Assert
        actual.Should().BeFalse();
    }
}
