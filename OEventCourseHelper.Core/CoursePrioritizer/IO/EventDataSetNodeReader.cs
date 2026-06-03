using OEventCourseHelper.Core.Data;
using OEventCourseHelper.Core.Xml;
using System.Collections.Immutable;
using System.Xml;

namespace OEventCourseHelper.Core.CoursePrioritizer.IO;

/// <summary>
/// Reads the courses from a IOF 3.0 Xml file and counts the total number of used controls.
/// </summary>
internal class EventDataSetNodeReader(CourseFilter Filter) : IXmlNodeReader
{
    private const string Namespace = "http://www.orienteering.org/datastandard/3.0";

    private const string ControlElementName = "Control";
    private const string ControlElementSchemaType = "Control";
    private const string CourseElementName = "Course";
    private const string CourseElementSchemaType = "Course";

    private ReaderState state = ReaderState.ReadControls;
    private readonly Dictionary<string, int> controlIndexer = [];
    private readonly List<Course> courseAccumulator = [];

    public Action<string>? OnValidationError { get; set; }

    /// <summary>
    /// Finalizes and returns the currently read data as an <see cref="EventDataSet"/>.
    /// </summary>
    /// <returns>An instance of <see cref="EventDataSet"/></returns>
    public EventDataSet GetEventDataSet()
    {
        var finalizedCourses = courseAccumulator
            .OrderBy(c => c.ControlMask, BitMask.NumericComparer.Instance)
            .ThenBy(c => c.CourseName, StringComparer.Ordinal)
            .Select((c, i) => c with
            {
                CourseIndex = i
            })
            .ToImmutableArray();

        var finalizedControls = controlIndexer
            .OrderBy(c => c.Value)
            .Select(c => c.Key)
            .ToImmutableArray();

        return new EventDataSet(finalizedControls, finalizedCourses);
    }

    /// <inheritdoc/>
    public bool CanRead(XmlReader reader)
    {
        if (reader.NodeType != XmlNodeType.Element)
        {
            return false;
        }

        return reader.LocalName switch
        {
            ControlElementName => reader.SchemaInfo?.SchemaType?.Name == ControlElementSchemaType,
            CourseElementName => reader.SchemaInfo?.SchemaType?.Name == CourseElementSchemaType,
            _ => false
        };
    }

    /// <inheritdoc/>
    public void Read(XmlReader reader)
    {
        switch (reader.LocalName)
        {
            case ControlElementName when state is ReaderState.ReadControls:
                ReadControl(reader);
                break;
            case CourseElementName when state is ReaderState.ReadControls:
                state = ReaderState.ReadCourses;
                SetCanonicalControlIndicies();
                ReadCourse(reader);
                break;
            case CourseElementName when state is ReaderState.ReadCourses:
                ReadCourse(reader);
                break;
            default:
                OnValidationError?.Invoke($"Validation Error: Element '{reader.LocalName}' encountered out of order.");
                return;
        }
    }

    private void ReadControl(XmlReader reader)
    {
        var typeAddr = reader.GetAttribute("type");
        if (typeAddr is not null && typeAddr != "Control")
        {
            return;
        }

        using var subReader = reader.ReadSubtree();
        while (subReader.Read())
        {
            if (subReader.NodeType == XmlNodeType.Element && subReader.LocalName == "Id")
            {
                var id = subReader.ReadElementContentAsString();
                controlIndexer.Add(id, -1);
                break;
            }
        }
    }

    private void ReadCourse(XmlReader reader)
    {
        using var subReader = reader.ReadSubtree();

        string? courseName = null;
        var controlCount = 0;
        var builder = new BitMask.Builder(BitMask.GetBucketCount(controlIndexer.Count));

        while (subReader.Read())
        {
            if (subReader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (subReader.LocalName)
            {
                case "Name":
                    courseName = subReader.ReadElementContentAsString();
                    break;
                case "CourseControl":
                    {
                        var typeAttr = subReader.GetAttribute("type");
                        if (typeAttr is not null && typeAttr != "Control")
                        {
                            continue;
                        }

                        using var ccReader = subReader.ReadSubtree();
                        while (ccReader.Read())
                        {
                            if (ccReader.NodeType != XmlNodeType.Element || ccReader.LocalName != "Control")
                            {
                                continue;
                            }

                            var controlCode = ccReader.ReadElementContentAsString();
                            if (!controlIndexer.TryGetValue(controlCode, out var index))
                            {
                                OnValidationError?.Invoke($"Validation Error: Course '{courseName}' references undefined control '{controlCode}'.");
                                return;
                            }

                            if (builder.Set(index))
                            {
                                controlCount++;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        if (courseName is not null)
        {
            var course = new Course(-1, courseName, builder.ToBitMask(), controlCount);
            if (Filter.Matches(course))
            {
                courseAccumulator.Add(course);
            }
        }
    }

    private void SetCanonicalControlIndicies()
    {
        var sortedKeys = controlIndexer.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < sortedKeys.Count; i++)
        {
            controlIndexer[sortedKeys[i]] = i;
        }
    }

    private enum ReaderState
    {
        Undefined = 0,
        ReadControls,
        ReadCourses,
    }
}
