using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Xml;
using System.Xml;
using System.Xml.Serialization;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

/// <summary>
/// Reads the courses from a IOF 3.0 Xml file and counts the total number of used controls.
/// </summary>
internal class EventDataSetNodeReader(CourseBuilderFilter Filter) : IXmlNodeReader
{
    private const string CourseElementName = "Course";
    private const string CourseElementSchemaType = "Course";

    private static readonly XmlSerializer courseSerializer = new(typeof(IOF.Xml.Course),
        new XmlRootAttribute(CourseElementName)
        {
            Namespace = "http://www.orienteering.org/datastandard/3.0"
        });

    private int currentIndex = 0;
    private readonly List<Course.Builder> courseBuilderAccumulator = [];
    private readonly Dictionary<string, int> controlIndexer = [];

    /// <summary>
    /// Finalizes and returns the currently read data as an <see cref="EventDataSet"/>.
    /// </summary>
    /// <returns>An instance of <see cref="EventDataSet"/></returns>
    public EventDataSet GetEventDataSet()
        => EventDataSet.Create(currentIndex, courseBuilderAccumulator);

    /// <inheritdoc/>
    public bool CanRead(XmlReader reader)
    {
        return reader.NodeType == XmlNodeType.Element
            && reader.LocalName == CourseElementName
            && reader.SchemaInfo?.SchemaType?.Name == CourseElementSchemaType;
    }

    /// <inheritdoc/>
    public void Read(XmlReader reader)
    {
        using var subReader = reader.ReadSubtree();
        var deserializedObject = courseSerializer.Deserialize(subReader);

        if (deserializedObject is IOF.Xml.Course iofCourse)
        {
            var builder = new Course.Builder(iofCourse.Name);
            foreach (var courseControl in iofCourse.CourseControl)
            {
                if (courseControl.type != IOF.Xml.ControlType.Control)
                {
                    continue;
                }

                if (courseControl.Control is null)
                {
                    continue;
                }

                foreach (var controlCode in courseControl.Control)
                {
                    if (!controlIndexer.TryGetValue(controlCode, out var index))
                    {
                        index = currentIndex++;
                        controlIndexer[controlCode] = index;
                    }

                    if (builder.ControlMaskBuilder.Set(index))
                    {
                        builder.ControlCount++;
                    }
                }
            }

            if (Filter.Matches(builder))
            {
                courseBuilderAccumulator.Add(builder);
            }
        }
    }
}
