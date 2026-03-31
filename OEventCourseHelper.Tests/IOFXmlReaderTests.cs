using FluentAssertions;
using OEventCourseHelper.Xml.Iof;

namespace OEventCourseHelper.Tests;

public class IOFXmlReaderTests
{
    [Fact]
    public void TryStream_ShouldCollectErrors_WhenMandatoryElementsAreMissing()
    {
        // Setup
        var invalidXml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <CourseData xmlns="http://www.orienteering.org/datastandard/3.0" iofVersion="3.0">
              <RaceCourseData />
            </CourseData>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, invalidXml);
            var reader = IOFXmlReader.Create();
            var dummyNodeReader = new DummyXmlNodeReader();

            // Act
            var success = reader.TryStream(tempFile, dummyNodeReader, out var errors);

            // Assert
            success.Should().BeFalse();
            errors.Should().NotBeNullOrEmpty();
            errors.Should().Contain(e => e.Contains("Event"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
