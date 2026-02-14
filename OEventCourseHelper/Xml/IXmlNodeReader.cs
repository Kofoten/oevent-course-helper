using System.Xml;

namespace OEventCourseHelper.Xml;

/// <summary>
/// Interface to implement for reading specific data when using the TryStream method of <see cref="Iof.IOFXmlReader"/>
/// </summary>
internal interface IXmlNodeReader
{
    /// <summary>
    /// Used to indicate if the current state of the inner <paramref name="reader"> can be read.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/></param>
    /// <returns>True if the current state can be read; otherwise False.</returns>
    public bool CanRead(XmlReader reader);

    /// <summary>
    /// Used to read data from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/></param>
    public void Read(XmlReader reader);
}
