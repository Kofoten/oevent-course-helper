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
    private int courseCount = 0;
    private int controlMaskBucketCount = 0;
    private readonly Dictionary<string, int> controlIndexer = [];
    private readonly List<(string courseName, int courseOffset, int controlCount)> courseAccumulator = [];
    private readonly BitMask.Builder courseMaskBuilder = new();

    public Action<string>? OnValidationError { get; set; }

    /// <summary>
    /// Finalizes and returns the currently read data as an <see cref="EventDataSet"/>.
    /// </summary>
    /// <returns>An instance of <see cref="EventDataSet"/></returns>
    public EventDataSet GetEventDataSet()
    {
        var finalizedCourseMask = courseMaskBuilder.ToBitMask(controlMaskBucketCount * courseCount);
        var comparer = new ArenaOffsetComparer(finalizedCourseMask, controlMaskBucketCount);

        var finalizedCoursesBuilder = ImmutableArray.CreateBuilder<Course>();
        var finalizedCourseNamesBuilder = ImmutableArray.CreateBuilder<string>();

        var orderedCourses = courseAccumulator
            .OrderBy(c => c.courseOffset, comparer)
            .ThenBy(c => c.courseName, StringComparer.Ordinal);

        var courseIndex = 0;
        foreach (var (courseName, courseOffset, controlCount) in orderedCourses)
        {
            finalizedCoursesBuilder.Add(new Course(courseIndex, courseOffset, controlCount));
            finalizedCourseNamesBuilder.Add(courseName);
            courseIndex++;
        }

        var finalizedControls = controlIndexer
            .OrderBy(c => c.Value)
            .Select(c => c.Key)
            .ToImmutableArray();

        return new EventDataSet(
            finalizedControls,
            finalizedCourseNamesBuilder.DrainToImmutable(),
            finalizedCoursesBuilder.DrainToImmutable(),
            finalizedCourseMask,
            controlMaskBucketCount);
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
        var courseOffset = controlMaskBucketCount * courseCount;

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

                            var controlIndex = courseOffset * 64 + index;
                            if (courseMaskBuilder.Set(controlIndex))
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


        if (courseName is null || !Filter.Matches(courseName, controlCount))
        {
            for (int i = courseOffset; i < courseOffset + controlMaskBucketCount; i++)
            {
                courseMaskBuilder.ClearBucket(i);
            }

            return;
        }

        courseCount++;
        courseAccumulator.Add((courseName, courseOffset, controlCount));
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

        controlMaskBucketCount = BitMask.GetBucketCount(controlIndexer.Count);
    }

    private enum ReaderState
    {
        Undefined = 0,
        ReadControls,
        ReadCourses,
    }

    public readonly struct ArenaOffsetComparer(BitMask courseMaskArena, int bucketCount) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            for (int i = bucketCount - 1; i >= 0; i--)
            {
                var xBucket = courseMaskArena.Buckets[x + i];
                var yBucket = courseMaskArena.Buckets[y + i];
                if (xBucket != yBucket)
                {
                    return xBucket.CompareTo(yBucket);
                }
            }

            return 0;
        }
    }
}
