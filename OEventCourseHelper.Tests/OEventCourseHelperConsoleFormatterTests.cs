using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Logging.Porcelain;
using OEventCourseHelper.Tests.TestUtilities;
using System.Text;

namespace OEventCourseHelper.Tests;

public class OEventCourseHelperConsoleFormatterTests
{
    private readonly IOptionsMonitor<OEventCourseHelperLoggingOptions> OptionsMonitor = new TestOptionsMonitor<OEventCourseHelperLoggingOptions>(new()
    {
        LoggingMode = OEventCourseHelperLoggingMode.Porcelain
    });

    [Theory]
    [InlineData("Simple", "Simple")]
    [InlineData("Course \"A\"", "Course \"\"A\"\"")]
    [InlineData("One, Two", "One, Two")]
    [InlineData("\"\"", "\"\"\"\"")]
    [InlineData("Line\r\nBreak", "Line Break")]
    [InlineData("Only\nNewline", "Only Newline")]
    [InlineData("Only\rCR", "OnlyCR")]
    [InlineData("Double\n\nSpace", "Double  Space")]
    public void WritePorcelainV1_ShouldCorrectlyEscapeValues(string rawInput, string expectedEscaped)
    {
        // Setup
        var formatter = new V1PorcelainFormatter();
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var eventId = new EventId(11003, "PriorityResult");
        var state = new List<KeyValuePair<string, object>>
        {
            new("courseName", rawInput),
            new("{OriginalFormat}", "{priority}. {courseName}")
        };

        var logEntry = new LogEntry<List<KeyValuePair<string, object>>>(
            LogLevel.Information,
            "Category",
            eventId,
            state,
            null,
            (s, e) => string.Empty);

        // Act
        formatter.Write(logEntry, writer);

        // Assert
        var result = output.ToString();
        // Contract: <LEVEL>:<ID>|<NAME>\t<KEY>="<VALUE>"\n
        result.Should().Be($"INF:11003|PriorityResult\tcourseName=\"{expectedEscaped}\"{Environment.NewLine}");
    }
}
