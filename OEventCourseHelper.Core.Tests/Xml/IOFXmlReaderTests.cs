using FluentAssertions;
using OEventCourseHelper.Core.Xml.Iof;
using OEventCourseHelper.TestUtilities;

namespace OEventCourseHelper.Core.Tests.Xml;

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

        var dummyNodeReader = new DummyXmlNodeReader();
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, invalidXml);
            using var stream = File.OpenRead(tempFile);
            using var iofReader = IOFXmlReader.Create(stream, dummyNodeReader);

            // Act
            var success = iofReader.TryStream();

            // Assert
            success.Should().BeFalse();
            iofReader.Errors.Should().NotBeNullOrEmpty();
            iofReader.Errors.Should().Contain(e => e.Contains("Event"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
