using OEventCourseHelper.Core.Xml;
using System.Xml;

namespace OEventCourseHelper.TestUtilities;

/// <summary>
/// A minimal reader that does nothing but satisfy the interface, 
/// allowing IOFXmlReader to perform its schema validation.
/// </summary>
public class DummyXmlNodeReader : IXmlNodeReader
{
    public Action<string>? OnValidationError { get; set; }

    public bool CanRead(XmlReader reader) => false;

    public void Read(XmlReader reader) { }
}
