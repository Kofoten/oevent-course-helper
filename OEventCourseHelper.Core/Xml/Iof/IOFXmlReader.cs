using System.Xml;
using System.Xml.Schema;

namespace OEventCourseHelper.Core.Xml.Iof;

public sealed class IOFXmlReader : IDisposable
{
    private const string XsdResourceName = "OEventCourseHelper.Core.Xml.Iof.IOF.xsd";

    private bool disposed = false;
    private bool consumed = false;

    private readonly XmlReader reader;
    private readonly IXmlNodeReader xmlNodeReader;
    private readonly List<string> errorCollector;

    public IReadOnlyList<string> Errors => errorCollector;

    private IOFXmlReader(
        XmlReader reader,
        IXmlNodeReader xmlNodeReader,
        List<string> errorCollector)
    {
        this.reader = reader;
        this.xmlNodeReader = xmlNodeReader;
        this.errorCollector = errorCollector;
    }

    /// <summary>
    /// Tries to read the specified IOF 3.0 Xml content by streaming it using an <see cref="IXmlNodeReader"/> to read and extract specific data from the file.
    /// </summary>
    /// <remarks>
    /// If any errors occured during the parsing they can be found in <see cref="Errors"/>
    /// </remarks>
    /// <returns>True if the file was read without issues; otherwise False.</returns>
    /// <exception cref="InvalidOperationException">If trying to call <see cref="TryStream"/> twice.</exception>
    public bool TryStream()
    {
        if (consumed)
        {
            throw new InvalidOperationException("The reader is consumed");
        }

        consumed = true;

        while (reader.Read())
        {
            if (xmlNodeReader.CanRead(reader))
            {
                xmlNodeReader.Read(reader);
            }
        }

        return errorCollector.Count == 0;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IOFXmlReader"/> pre loaded with the schema file for IOF 3.0 Xml files.
    /// </summary>
    /// <returns>A new instance of <see cref="IOFXmlReader"/></returns>
    /// <param name="stream">The stream to read.</param>
    /// <param name="xmlNodeReader">The node reader to use for parsing.</param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="AggregateException"></exception>
    public static IOFXmlReader Create(Stream stream, IXmlNodeReader xmlNodeReader)
    {
        var errorCollector = new List<string>();
        var settings = CreateXmlReaderSettings();

        settings.ValidationEventHandler += (sender, e) =>
        {
            errorCollector.Add(e.Message);
        };

        xmlNodeReader.OnValidationError = errorCollector.Add;

        var reader = XmlReader.Create(stream, settings);
        return new IOFXmlReader(reader, xmlNodeReader, errorCollector);
    }

    private static XmlReaderSettings CreateXmlReaderSettings()
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

        var settings = new XmlReaderSettings
        {
            Schemas = schemas,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema
                            | XmlSchemaValidationFlags.ReportValidationWarnings
                            | XmlSchemaValidationFlags.ProcessIdentityConstraints,
        };

        return settings;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            reader.Dispose();
            disposed = true;
        }
    }
}
