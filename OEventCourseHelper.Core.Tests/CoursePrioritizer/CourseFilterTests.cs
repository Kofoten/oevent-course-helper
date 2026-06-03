using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer.IO;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CourseFilterTests
{
    [Fact]
    public void Matches_ShouldMatchCourse()
    {
        // Setup
        var filter = new CourseFilter(true, ["Course"]);

        // Act
        var actual = filter.Matches("Course", 1);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldNotMatchEmptyCourses()
    {
        // Setup
        var filter = new CourseFilter(true, []);

        // Act
        var actual = filter.Matches("Empty", 0);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldNotMatchCourseNotContainingAnyFilterString()
    {
        // Setup
        var filter = new CourseFilter(false, ["Course"]);

        // Act
        var actual = filter.Matches("NoMatch", 1);

        // Assert
        actual.Should().BeFalse();
    }
}
