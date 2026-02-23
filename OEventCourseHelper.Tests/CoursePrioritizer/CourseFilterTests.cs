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
        var maskBuilder = new BitMask.Builder();
        maskBuilder.Set(0);

        var builder = new Course.Builder()
        {
            CourseName = "Course",
            ControlMaskBuilder = maskBuilder,
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
        var maskBuilder = new BitMask.Builder();

        var builder = new Course.Builder()
        {
            CourseName = "Empty",
            ControlMaskBuilder = maskBuilder,
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
        var maskBuilder = new BitMask.Builder();
        maskBuilder.Set(2);

        var builder = new Course.Builder()
        {
            CourseName = "NoMatch",
            ControlMaskBuilder = maskBuilder,
            ControlCount = 1,
        };

        var filter = new CourseBuilderFilter(false, ["Course"]);

        // Act
        var actual = filter.Matches(builder);

        // Assert
        actual.Should().BeFalse();
    }
}
