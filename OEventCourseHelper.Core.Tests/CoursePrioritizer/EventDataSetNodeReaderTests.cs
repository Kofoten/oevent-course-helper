using FluentAssertions;
using OEventCourseHelper.Core.CoursePrioritizer.IO;
using OEventCourseHelper.Core.Data;
using OEventCourseHelper.Core.Xml.Iof;

namespace OEventCourseHelper.Core.Tests.CoursePrioritizer;

public class EventDataSetNodeReaderTests
{
    private const string SampleXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<CourseData xmlns=""http://www.orienteering.org/datastandard/3.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" iofVersion=""3.0"">
  <Event>
    <Name>The test event</Name>
  </Event>
  <RaceCourseData>
    <Control type=""Start""><Id>S1</Id></Control>
    <Control type=""Control""><Id>32</Id></Control>
    <Control type=""Control""><Id>31</Id></Control>
    <Course>
      <Name>TestCourse1</Name>
      <CourseControl type=""Start""><Control>S1</Control></CourseControl>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
      <CourseControl type=""Control""><Control>32</Control></CourseControl>
    </Course>
    <Course>
      <Name>TestCourse2</Name>
      <CourseControl type=""Start""><Control>S1</Control></CourseControl>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
      <CourseControl type=""Control""><Control>32</Control></CourseControl>
    </Course>
    <Course>
      <Name>TestCourse3</Name>
      <CourseControl type=""Start""><Control>S1</Control></CourseControl>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
    </Course>
  </RaceCourseData>
</CourseData>";

    [Fact]
    public void Read_ShouldCorrectlyIndexControlsAndCourses()
    {
        // Setup
        var filter = new CourseFilter(FilterEmpty: true, NameIncludes: []);
        var reader = new EventDataSetNodeReader(filter);
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, SampleXml);
            using var stream = File.OpenRead(tempPath);
            using var iofReader = IOFXmlReader.Create(stream, reader);

            // Act
            var success = iofReader.TryStream();

            // Assert
            success.Should().BeTrue();
            iofReader.Errors.Should().BeEmpty();

            var dataSet = reader.GetEventDataSet();
            dataSet.Controls.Should().HaveCount(2);
            dataSet.Controls.Should().BeEquivalentTo(["31", "32"], conf => conf.WithStrictOrdering());
            dataSet.Courses.Should().HaveCount(3);
            dataSet.Courses[0].CourseIndex.Should().Be(0);
            dataSet.Courses[0].CourseName.Should().Be("TestCourse3");
            dataSet.Courses[0].ControlMask.Should().Be(new BitMask([0b01UL]));
            dataSet.Courses[1].CourseIndex.Should().Be(1);
            dataSet.Courses[1].CourseName.Should().Be("TestCourse1");
            dataSet.Courses[1].ControlMask.Should().Be(new BitMask([0b11UL]));
            dataSet.Courses[2].CourseIndex.Should().Be(2);
            dataSet.Courses[2].CourseName.Should().Be("TestCourse2");
            dataSet.Courses[2].ControlMask.Should().Be(new BitMask([0b11UL]));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Read_ShouldYieldError_WhenElementsAreOutOfOrder()
    {
        // Setup
        var outOfOrderXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<CourseData xmlns=""http://www.orienteering.org/datastandard/3.0"" iofVersion=""3.0"">
  <Event>
    <Name>The test event</Name>
  </Event>
  <RaceCourseData>
    <Control type=""Control""><Id>31</Id></Control>
    <Control type=""Control""><Id>32</Id></Control>
    <Course>
      <Name>TestCourse</Name>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
      <CourseControl type=""Control""><Control>32</Control></CourseControl>
    </Course>
    <Control type=""Control""><Id>33</Id></Control>
  </RaceCourseData>
</CourseData>";

        var filter = new CourseFilter(FilterEmpty: false, NameIncludes: []);
        var reader = new EventDataSetNodeReader(filter);
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, outOfOrderXml);
            using var stream = File.OpenRead(tempPath);
            using var iofReader = IOFXmlReader.Create(stream, reader);

            // Act
            var success = iofReader.TryStream();

            // Assert
            success.Should().BeFalse();
            iofReader.Errors.Should().NotBeNull();
            iofReader.Errors.Should().ContainSingle(e => e == "Validation Error: Element 'Control' encountered out of order."
                ||
                e.StartsWith("The element 'RaceCourseData' in namespace 'http://www.orienteering.org/datastandard/3.0' has invalid child element 'Control' in namespace 'http://www.orienteering.org/datastandard/3.0'."));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Read_ShouldYieldError_WhenCourseReferencesUndefinedControl()
    {
        // Setup
        var undefinedControlXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<CourseData xmlns=""http://www.orienteering.org/datastandard/3.0"" iofVersion=""3.0"">
  <Event>
    <Name>The test event</Name>
  </Event>
  <RaceCourseData>
    <Control type=""Control""><Id>31</Id></Control>
    <Course>
      <Name>TestCourse</Name>
      <CourseControl type=""Control""><Control>31</Control></CourseControl>
      <CourseControl type=""Control""><Control>999</Control></CourseControl>
    </Course>
  </RaceCourseData>
</CourseData>";

        var filter = new CourseFilter(FilterEmpty: false, NameIncludes: []);
        var reader = new EventDataSetNodeReader(filter);
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, undefinedControlXml);
            using var stream = File.OpenRead(tempPath);
            using var iofReader = IOFXmlReader.Create(stream, reader);

            // Act
            var success = iofReader.TryStream();

            // Assert
            success.Should().BeFalse();
            iofReader.Errors.Should().NotBeNull();
            iofReader.Errors.Should().ContainSingle(e => e == "Validation Error: Course 'TestCourse' references undefined control '999'.");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Read_ShouldYieldError_WhenMandatoryEventElementIsMissing()
    {
        // Setup
        var missingEventXml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<CourseData xmlns=""http://www.orienteering.org/datastandard/3.0"" iofVersion=""3.0"">
  <RaceCourseData>
    <Control type=""Start""><Id>S1</Id></Control>
  </RaceCourseData>
</CourseData>";

        var filter = new CourseFilter(FilterEmpty: false, NameIncludes: []);
        var reader = new EventDataSetNodeReader(filter);
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, missingEventXml);
            using var stream = File.OpenRead(tempPath);
            using var iofReader = IOFXmlReader.Create(stream, reader);

            // Act
            var success = iofReader.TryStream();

            // Assert
            success.Should().BeFalse();
            iofReader.Errors.Should().NotBeNullOrEmpty();
            iofReader.Errors.Should().ContainSingle(e => e == "The element 'CourseData' in namespace 'http://www.orienteering.org/datastandard/3.0' has invalid child element 'RaceCourseData' in namespace 'http://www.orienteering.org/datastandard/3.0'. List of possible elements expected: 'Event' in namespace 'http://www.orienteering.org/datastandard/3.0'.");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
