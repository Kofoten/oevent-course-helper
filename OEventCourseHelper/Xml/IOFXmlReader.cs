using IOF.Xml;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace OEventCourseHelper.Xml;

internal sealed class IOFXmlReader
{
    private const string XsdResourceName = "OEventCourseHelper.Xml.IOF.xsd";

    private readonly XmlSchemaSet schemas;

    private IOFXmlReader(XmlSchemaSet schemas)
    {
        this.schemas = schemas;
    }

    public bool TryLoad(
        string iofXmlPath,
        [NotNullWhen(true)] out CourseData? courseData,
        [NotNullWhen(false)] out List<string>? errors)
    {
        if (!File.Exists(iofXmlPath))
        {
            errors = [$"The file '{iofXmlPath}' could not be found."];
            courseData = null;
            return false;
        }

        var validationMessages = new List<string>();
        using var reader = CreateInnerXmlReader(iofXmlPath, validationMessages);
        var serializer = new XmlSerializer(typeof(CourseData));
        var xmlContent = serializer.Deserialize(reader);

        if (validationMessages.Count > 0)
        {
            errors = validationMessages;
            courseData = null;
            return false;
        }

        if (xmlContent is not CourseData cd)
        {
            errors = [$"The file '{iofXmlPath}' could not be loaded."];
            courseData = null;
            return false;
        }

        errors = null;
        courseData = cd;
        return true;
    }

    public bool TryStream<T>(
        string iofXmlPath,
        IXmlNodeReader<T> xmlNodeReader,
        [NotNullWhen(false)] out List<string>? errors)
    {
        if (!File.Exists(iofXmlPath))
        {
            errors = [$"The file '{iofXmlPath}' could not be found."];
            return false;
        }

        var validationMessages = new List<string>();
        using var reader = CreateInnerXmlReader(iofXmlPath, validationMessages);

        while (reader.Read())
        {
            if (xmlNodeReader.CanRead(reader))
            {
                xmlNodeReader.Read(reader);
            }
        }

        if (validationMessages.Count > 0)
        {
            errors = validationMessages;
            return false;
        }

        errors = null;
        return true;
    }

    private XmlReader CreateInnerXmlReader(string iofXmlPath, IList<string> validationMessageCollector)
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

        return XmlReader.Create(iofXmlPath, settings);
    }

    internal static IOFXmlReader Create()
    {
        var xsdErrors = new List<XmlSchemaException>();
        using var xsdStream = Assembly.GetExecutingAssembly()
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
