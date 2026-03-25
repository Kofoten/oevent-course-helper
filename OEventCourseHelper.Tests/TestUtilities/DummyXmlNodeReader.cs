using OEventCourseHelper.Xml;

using System.Xml;

/// <summary>
/// A minimal reader that does nothing but satisfy the interface, 
/// allowing IOFXmlReader to perform its schema validation.
/// </summary>
internal class DummyXmlNodeReader : IXmlNodeReader
{
    public Action<string>? OnValidationError { get; set; }

    public bool CanRead(XmlReader reader) => false;

    public void Read(XmlReader reader) { }
}
