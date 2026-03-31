using System.Xml;
using System.Xml.Schema;

namespace OEventCourseHelper.Core.Xml.Iof;

public sealed class IOFXmlReader
{
    private const string XsdResourceName = "OEventCourseHelper.Core.Xml.Iof.IOF.xsd";

    private readonly XmlSchemaSet schemas;

    private IOFXmlReader(XmlSchemaSet schemas)
    {
        this.schemas = schemas;
    }

    /// <summary>
    /// Tries to read the specified IOF 3.0 Xml file by streaming it using an <see cref="IXmlNodeReader"/> to read and extract specific data from the file.
    /// </summary>
    /// <param name="iofXmlPath">The path to the IOF 3.0 Xml file.</param>
    /// <param name="xmlNodeReader">The node reader to use for reading.</param>
    /// <param name="errors">Returns any errors that occured while reading the file.</param>
    /// <returns>True if the file was read without issues; otherwise False.</returns>
    public bool TryStreamFile(
        string iofXmlPath,
        IXmlNodeReader xmlNodeReader,
        out List<string> errors)
    {
        if (!File.Exists(iofXmlPath))
        {
            errors = [$"The file '{iofXmlPath}' could not be found."];
            return false;
        }

        errors = [];
        xmlNodeReader.OnValidationError = errors.Add;
        var readerSettings = CreateXmlReaderSettings(errors);
        using var reader = XmlReader.Create(iofXmlPath, readerSettings);

        StreamInternal(reader, xmlNodeReader);
        return errors.Count == 0;
    }

    /// <summary>
    /// Tries to read the specified IOF 3.0 Xml content by streaming it using an <see cref="IXmlNodeReader"/> to read and extract specific data from the file.
    /// </summary>
    /// <param name="iofXmlContent">The IOF 3.0 Xml content to parse.</param>
    /// <param name="xmlNodeReader">The node reader to use for reading.</param>
    /// <param name="errors">Returns any errors that occured while reading the file.</param>
    /// <returns>True if the file was read without issues; otherwise False.</returns>
    public bool TryStreamString(
        string iofXmlContent,
        IXmlNodeReader xmlNodeReader,
        out List<string> errors)
    {
        errors = [];
        xmlNodeReader.OnValidationError = errors.Add;
        var readerSettings = CreateXmlReaderSettings(errors);
        using var sr = new StringReader(iofXmlContent);
        using var reader = XmlReader.Create(sr, readerSettings);

        StreamInternal(reader, xmlNodeReader);
        return errors.Count == 0;
    }

    private static void StreamInternal(XmlReader reader, IXmlNodeReader xmlNodeReader)
    {
        while (reader.Read())
        {
            if (xmlNodeReader.CanRead(reader))
            {
                xmlNodeReader.Read(reader);
            }
        }
    }

    private XmlReaderSettings CreateXmlReaderSettings(List<string> validationMessageCollector)
    {
        var settings = new XmlReaderSettings
        {
            Schemas = schemas,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema
                            | XmlSchemaValidationFlags.ReportValidationWarnings
                            | XmlSchemaValidationFlags.ProcessIdentityConstraints,
        };

        settings.ValidationEventHandler += (sender, e) =>
        {
            validationMessageCollector.Add(e.Message);
        };

        return settings;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IOFXmlReader"/> pre loaded with the schema file for IOF 3.0 Xml files. This instance is reusable.
    /// </summary>
    /// <returns>A new instance of <see cref="IOFXmlReader"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="AggregateException"></exception>
    public static IOFXmlReader Create()
    {
        var xsdErrors = new List<XmlSchemaException>();
        using var xsdStream = typeof(IOFXmlReader).Assembly
            .GetManifestResourceStream(XsdResourceName);

        if (xsdStream is null)
        {
            throw new InvalidOperationException($"The embedded resource '{XsdResourceName}' could not be loaded.");
        }

        var schema = XmlSchema.Read(xsdStream, (sender, e) => { xsdErrors.Add(e.Exception); });

        if (xsdErrors.Count > 0)
        {
            throw new AggregateException($"There were errors while reading the schema {XsdResourceName}.", xsdErrors);
        }

        if (schema is null)
        {
            throw new InvalidOperationException($"The embedded resource '{XsdResourceName}' could not be loaded.");
        }

        var schemas = new XmlSchemaSet();
        schemas.Add(schema);

        return new IOFXmlReader(schemas);
    }
}
