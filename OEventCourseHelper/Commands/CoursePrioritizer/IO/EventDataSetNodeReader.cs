using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Xml;
using Spectre.Console;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Serialization;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

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

    private static readonly XmlSerializer controlSerializer = new(
        typeof(IOF.Xml.Control),
        new XmlRootAttribute(ControlElementName)
        {
            Namespace = Namespace,
        });

    private static readonly XmlSerializer courseSerializer = new(
        typeof(IOF.Xml.Course),
        new XmlRootAttribute(CourseElementName)
        {
            Namespace = Namespace
        });

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
        using var subReader = reader.ReadSubtree();
        var deserializedObject = controlSerializer.Deserialize(subReader);

        if (deserializedObject is IOF.Xml.Control iofControl)
        {
            if (iofControl.type != IOF.Xml.ControlType.Control)
            {
                return;
            }

            controlIndexer.TryAdd(iofControl.Id.Value, -1);
        }
    }

    private void ReadCourse(XmlReader reader)
    {
        using var subReader = reader.ReadSubtree();
        var deserializedObject = courseSerializer.Deserialize(subReader);

        if (deserializedObject is IOF.Xml.Course iofCourse)
        {
            var controlCount = 0;
            var builder = new BitMask.Builder(BitMask.GetBucketCount(controlIndexer.Count));
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
                        OnValidationError?.Invoke($"Validation Error: Course '{iofCourse.Name}' references undefined control '{controlCode}'.");
                        return;
                    }

                    if (builder.Set(index))
                    {
                        controlCount++;
                    }
                }
            }

            var course = new Course(-1, iofCourse.Name, builder.ToBitMask(), controlCount);
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
