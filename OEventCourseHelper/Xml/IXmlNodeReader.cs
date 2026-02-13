using System.Xml;

namespace OEventCourseHelper.Xml;

internal interface IXmlNodeReader
{
    public bool CanRead(XmlReader reader);
    public void Read(XmlReader reader);
}
