using System.Xml;

namespace OEventCourseHelper.Core.Xml;

/// <summary>
/// Interface to implement for reading specific data when using the TryStream method of <see cref="Iof.IOFXmlReader"/>
/// </summary>
public interface IXmlNodeReader
{
    /// <summary>
    /// A callback that can be used by the reader to inject a handler that receives custom validation errors.
    /// </summary>
    Action<string>? OnValidationError { get; set; }

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
