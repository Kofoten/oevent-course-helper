using OEventCourseHelper.Commands.CoursePrioritizer.Data;
using OEventCourseHelper.Extensions;
using OEventCourseHelper.Xml;
using System.Xml;

namespace OEventCourseHelper.Commands.CoursePrioritizer.IO;

internal class CourseMaskNodeReader : IXmlNodeReader
{
    private const string CourseElementName = "Course";
    private const string CourseNameElementName = "Name";
    private const string CourseControlElementName = "CourseControl";
    private const string ControlElementName = "Control";
    private const string ControlTypeAttributeName = "type";
    private const string ValidControlTypeAttributeValue = "Control";

    private int currentIndex = 0;
    private readonly List<CourseMaskBuilder> courseBuilderAccumulator = [];
    private readonly Dictionary<string, int> controlIndexer = [];

    public CourseMaskNodeReaderResult GetResult() => new()
    {
        CourseMasks = GetCourseMasks(),
        TotalEventControlCount = currentIndex,
    };

    public bool CanRead(XmlReader reader)
    {
        return reader.NodeType == XmlNodeType.Element && reader.LocalName == CourseElementName;
    }

    public void Read(XmlReader reader)
    {
        using var subReader = reader.ReadSubtree();
        subReader.Read();

        var builder = new CourseMaskBuilder();
        while (subReader.Read())
        {
            if (subReader.NodeType == XmlNodeType.Element)
            {
                if (subReader.LocalName == CourseNameElementName)
                {
                    builder.CourseName = subReader.ReadElementContentAsString();
                }
                else if (subReader.LocalName == CourseControlElementName)
                {
                    string type = subReader.GetAttribute(ControlTypeAttributeName)
                        ?? ValidControlTypeAttributeValue;

                    if (type == ValidControlTypeAttributeValue)
                    {
                        builder.ControlCount += ProcessCourseControl(subReader, builder.ControlMask);
                    }
                }
            }
        }

        courseBuilderAccumulator.Add(builder);
    }

    private int ProcessCourseControl(XmlReader reader, IList<ulong> controlMask)
    {
        var controlCount = 0;
        if (reader.IsEmptyElement)
        {
            return controlCount;
        }

        int initialDepth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == initialDepth)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == ControlElementName)
            {
                var code = reader.ReadElementContentAsString();
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                if (!controlIndexer.TryGetValue(code, out var index))
                {
                    index = currentIndex++;
                    controlIndexer[code] = index;
                }

                int wordIndex = index >> 6;
                int bitIndex = index & 63;

                while (controlMask.Count <= wordIndex)
                {
                    controlMask.Add(0UL);
                }

                ulong mask = 1UL << bitIndex;
                if ((controlMask[wordIndex] & mask) == 0)
                {
                    controlMask[wordIndex] |= mask;
                    controlCount++;
                }
            }
        }

        return controlCount;
    }

    private IEnumerable<CourseMask> GetCourseMasks()
    {
        if (currentIndex == 0)
        {
            yield break;
        }

        var bucketCount = currentIndex.GetUnsignedLongBucketCount();
        foreach (var builder in courseBuilderAccumulator)
        {
            yield return builder.ToCourseMask(bucketCount);
        }
    }
}
