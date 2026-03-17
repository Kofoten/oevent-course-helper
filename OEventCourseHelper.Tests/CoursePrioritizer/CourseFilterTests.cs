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
        var course = new Course(0, "Course", new([0b1UL]), 1);
        var filter = new CourseFilter(true, ["Course"]);

        // Act
        var actual = filter.Matches(course);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Matches_ShouldNotMatchEmptyCourses()
    {
        // Setup
        var course = new Course(0, "Empty", new([]), 0);
        var filter = new CourseFilter(true, []);

        // Act
        var actual = filter.Matches(course);

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Matches_ShouldNotMatchCourseNotContainingAnyFilterString()
    {
        // Setup
        var course = new Course(0, "NoMatch", new([0b10UL]), 1);
        var filter = new CourseFilter(false, ["Course"]);

        // Act
        var actual = filter.Matches(course);

        // Assert
        actual.Should().BeFalse();
    }
}
