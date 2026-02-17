using FluentAssertions;
using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Commands.CoursePrioritizer.IO;

namespace OEventCourseHelper.Tests.CoursePrioritizer;

public class CourseMaskFilterTests
{
    [Fact]
    public void Filter_ShouldRemoveEmptyCourses()
    {
        // Setup
        var courseMasks = new CourseMask[]
        {
            new("Course 1", [1Ul], 1),
            new("Course 2", [0Ul], 0),
            new("Course 3", [2Ul], 1),
        };

        var filter = new CourseMaskBuilderFilter(true, ["Course"]);

        // Act
        var actual = filter.Filter(courseMasks).ToList();

        // Assert
        actual.Should().HaveCount(2);
        actual[0].CourseName.Should().Be("Course 1");
        actual[1].CourseName.Should().Be("Course 3");
    }

    [Fact]
    public void Filter_ShouldRemoveCourseNotContainingAnyFilterString()
    {
        // Setup
        var courseMasks = new CourseMask[]
        {
            new("Course 1", [1Ul], 1),
            new("Cours 2", [4Ul], 1),
            new("Course 3", [2Ul], 1),
        };

        var filter = new CourseMaskBuilderFilter(true, ["Course"]);

        // Act
        var actual = filter.Filter(courseMasks).ToList();

        // Assert
        actual.Should().HaveCount(2);
        actual[0].CourseName.Should().Be("Course 1");
        actual[1].CourseName.Should().Be("Course 3");
    }
}
