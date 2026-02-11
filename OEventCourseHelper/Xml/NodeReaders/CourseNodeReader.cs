using OEventCourseHelper.Data;
using System.Collections.Frozen;
using System.Xml;

namespace OEventCourseHelper.Xml.NodeReaders;

internal class CourseNodeReader : IXmlNodeReader<Course>
{
    private readonly List<Course> courseAccumulator = [];

    public FrozenSet<Course> Courses => courseAccumulator.ToFrozenSet();

    public bool CanRead(XmlReader reader)
    {
        return reader.NodeType == XmlNodeType.Element && reader.LocalName == "Course";
    }

    public void Read(XmlReader reader)
    {
        using var subReader = reader.ReadSubtree();
        subReader.Read();

        string name = "Unknown Course";
        var controls = new HashSet<string>();

        while (subReader.Read())
        {
            if (subReader.NodeType == XmlNodeType.Element)
            {
                if (subReader.LocalName == "Name")
                {
                    name = subReader.ReadElementContentAsString();
                }
                else if (subReader.LocalName == "CourseControl")
                {
                    string type = subReader.GetAttribute("type") ?? "Control";

                    if (type == "Control")
                    {
                        ProcessCourseControl(subReader, controls);
                    }
                }
            }
        }

        courseAccumulator.Add(new Course(name, controls.ToFrozenSet()));
    }

    private static void ProcessCourseControl(XmlReader reader, HashSet<string> controls)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        int initialDepth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == initialDepth)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Control")
            {
                var code = reader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    controls.Add(code);
                }
            }
        }
    }
}
