using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer;
using System.Text;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class CoursePrioritizerEngineTests
{
    private const string OrphanedControlXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<CourseData xmlns=""http://www.orienteering.org/datastandard/3.0"" iofVersion=""3.0"">
  <Event><Name>Test</Name></Event>
  <RaceCourseData>
    <Control type=""Control""><Id>31</Id></Control>
    <Control type=""Control""><Id>32</Id></Control>
    <Control type=""Control""><Id>33</Id></Control>
    <Course>
      <Name>Course 1</Name>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
      <CourseControl type=""Control""><Control>32</Control></CourseControl>
    </Course>
  </RaceCourseData>
</CourseData>";

    [Fact]
    public void Execute_ShouldReturnValidationFailed_WhenStrictIsEnabledAndControlsAreSkipped()
    {
        // Setup
        var engine = new CoursePrioritizerEngine(BeamWidth: 3, Strict: true, Filters: []);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(OrphanedControlXml));

        // Act
        var result = engine.Run(stream);

        // Assert
        var error = result.Should().BeOfType<CoursePrioritizerResult.ValidationFailure>().Which;

        error.ValidationInfo.SkippedControls.Should().ContainSingle("33");
    }

    [Fact]
    public void Execute_ShouldWarnSkippedControl_WhenStrictIsDisabledAndControlsAreSkipped()
    {
        // Setup
        var engine = new CoursePrioritizerEngine(BeamWidth: 3, Strict: false, Filters: []);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(OrphanedControlXml));

        // Act
        var result = engine.Run(stream);

        // Assert
        var success = result.Should().BeOfType<CoursePrioritizerResult.Success>().Which;

        success.ValidationInfo.SkippedControls.Should().ContainSingle("33");
        success.Summary.VisitedControlCount.Should().Be(2);
        success.Summary.TotalControlCount.Should().Be(3);
    }
}
