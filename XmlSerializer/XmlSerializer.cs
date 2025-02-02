using System.IO;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <summary>
    /// Serializes and deserializes <typeparamref name="T"/> objects into XML documents.
    /// Allows you to control the process of encoding objects in XML.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    public static class XmlSerializer<T>
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(T));
        private static readonly XmlWriterSettings _defaultWriterSettings = new XmlWriterSettings
        {
            CheckCharacters = false,
            CloseOutput = false,
            ConformanceLevel = ConformanceLevel.Auto,
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "\t",
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
            NewLineChars = "\r\n",
            NewLineHandling = NewLineHandling.Replace,
            NewLineOnAttributes = false,
            OmitXmlDeclaration = false
        };
        private static readonly XmlReaderSettings _defaultReaderSettings = new XmlReaderSettings
        {
            CheckCharacters = false,
            CloseInput = false,
            ConformanceLevel = ConformanceLevel.Auto,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            LineNumberOffset = 0,
            LinePositionOffset = 0,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = 0,
            NameTable = null,
            // Schemas = null, // ???
            ValidationFlags = XmlSchemaValidationFlags.None,
            ValidationType = ValidationType.None,
            XmlResolver = null
        };

        /// <summary>
        /// Default character encoding.
        /// </summary>
        public static Encoding DefaultEncoding => Encoding.UTF8;

        /// <summary>
        /// Default settings for the <see cref="XmlWriter" /> instance being created.
        /// </summary>
        public static XmlWriterSettings DefaultXmlWriterSettings => _defaultWriterSettings.Clone();

        /// <summary>
        /// Default settings for the <see cref="XmlReader" /> instance that is created.
        /// </summary>
        public static XmlReaderSettings DefaultXmlReaderSettings => _defaultReaderSettings.Clone();

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and returns the XML document as a string.
        /// </summary>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        public static string Serialize(T o)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(sb))
                _serializer.Serialize(xmlWriter, o, (XmlSerializerNamespaces)null);
            return sb.ToString();
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and returns the XML document as a string.
        /// </summary>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlWriterSettings"/>.
        /// </param>
        public static string Serialize(T o, XmlWriterSettings settings)
        {
            if (settings == null) settings = _defaultWriterSettings;
            StringBuilder sb = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(sb, settings))
                _serializer.Serialize(xmlWriter, o, (XmlSerializerNamespaces)null);
            return sb.ToString();
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="TextWriter" />.
        /// </summary>
        /// <param name="textWriter">
        /// <see cref="TextWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        public static void Serialize(TextWriter textWriter, T o)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter))
                _serializer.Serialize(textWriter, o, (XmlSerializerNamespaces)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="TextWriter" />.
        /// </summary>
        /// <param name="textWriter">
        /// <see cref="TextWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlWriterSettings"/>.
        /// </param>
        public static void Serialize(TextWriter textWriter, T o, XmlWriterSettings settings)
        {
            if (settings == null) settings = _defaultWriterSettings;
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                _serializer.Serialize(xmlWriter, o, (XmlSerializerNamespaces)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="TextWriter" /> and references to the given namespaces.
        /// </summary>
        /// <param name="textWriter">
        /// <see cref="TextWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> Contains the namespaces for the generated XML document.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(TextWriter textWriter, T o, XmlSerializerNamespaces namespaces)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter))
                _serializer.Serialize(xmlWriter, o, namespaces);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="TextWriter" /> and references to the given namespaces.
        /// </summary>
        /// <param name="textWriter">
        /// <see cref="TextWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> Contains the namespaces for the generated XML document.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlWriterSettings"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(TextWriter textWriter, T o, XmlSerializerNamespaces namespaces, XmlWriterSettings settings)
        {
            if (settings == null) settings = _defaultWriterSettings;
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                _serializer.Serialize(xmlWriter, o, namespaces);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(Stream stream, T o)
        {
            _serializer.Serialize(stream, o, (XmlSerializerNamespaces)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlWriterSettings"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(Stream stream, T o, XmlWriterSettings settings)
        {
            if (settings == null) settings = _defaultWriterSettings;
            using (TextWriter writer = new StreamWriter(stream, settings.Encoding))
            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                _serializer.Serialize(xmlWriter, o, (XmlSerializerNamespaces)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="Stream" /> refers to the given namespaces.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> The object is referenced.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(Stream stream, T o, XmlSerializerNamespaces namespaces)
        {
            _serializer.Serialize(stream, o, namespaces);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="Stream" /> refers to the given namespaces.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> The object is referenced.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlWriterSettings"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(Stream stream, T o, XmlSerializerNamespaces namespaces, XmlWriterSettings settings)
        {
            if (settings == null) settings = _defaultWriterSettings;
            using (TextWriter writer = new StreamWriter(stream, settings.Encoding))
            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                _serializer.Serialize(xmlWriter, o, namespaces);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="XmlWriter" />.
        /// </summary>
        /// <param name="xmlWriter">
        /// <see cref="XmlWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// the settings of the current <see cref="XmlWriter" /> instance are used.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(XmlWriter xmlWriter, T o, XmlWriterSettings settings = null)
        {
            using (XmlWriter writer = settings == null ? xmlWriter : XmlWriter.Create(xmlWriter, settings))
                _serializer.Serialize(writer, o, (XmlSerializerNamespaces)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="XmlWriter" /> and references to the given namespaces.
        /// </summary>
        /// <param name="xmlWriter">
        /// <see cref="XmlWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">
        /// An instance <typeparamref name="T"/> to serialize.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> The object is referenced.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// the settings of the current <see cref="XmlWriter" /> instance are used.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(XmlWriter xmlWriter, T o, XmlSerializerNamespaces namespaces, XmlWriterSettings settings = null)
        {
            using (XmlWriter writer = settings == null ? xmlWriter : XmlWriter.Create(xmlWriter, settings))
                _serializer.Serialize(writer, o, namespaces);
        }

        /// <summary>
        /// Serializes the specified object and writes the XML document to a file using the specified <typeparamref name="T"/> and references the specified namespaces and encoding style.
        /// </summary>
        /// <param name="xmlWriter">
        /// <see cref="XmlWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">Object to serialize.</param>
        /// <param name="namespaces">
        /// <see cref="XmlSerializerNamespaces" /> The object is referenced.
        /// </param>
        /// <param name="encodingStyle">
        /// The encoding style of the serialized XML.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// the settings of the current <see cref="XmlWriter" /> instance are used.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during serialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static void Serialize(
            XmlWriter xmlWriter,
            T o,
            XmlSerializerNamespaces namespaces,
            string encodingStyle,
            XmlWriterSettings settings = null)
        {
            using (XmlWriter writer = settings == null ? xmlWriter : XmlWriter.Create(xmlWriter, settings))
                _serializer.Serialize(writer, o, namespaces, encodingStyle, (string)null);
        }

        /// <summary>
        /// Serializes the given <typeparamref name="T"/> and writes the XML document to a file using the given <see cref="XmlWriter" />, XML namespaces, and encoding.
        /// </summary>
        /// <param name="xmlWriter">
        /// <see cref="XmlWriter" /> Used to write an XML document.
        /// </param>
        /// <param name="o">Object to serialize.</param>
        /// <param name="namespaces">
        /// An instance of <see langword="XmlSerializaerNamespaces" /> containing the namespaces and prefixes used.
        /// </param>
        /// <param name="encodingStyle">
        /// The encoding used in the document.
        /// </param>
        /// <param name="id">
        /// For SOAP encoded messages, a base is used to generate identifier attributes.
        /// </param>
        /// <param name="settings">
        /// Settings for the new <see cref="XmlWriter" /> instance. If <see langword="null"/> is specified,
        /// the settings of the current <see cref="XmlWriter" /> instance are used.
        /// </param>
        public static void Serialize(
            XmlWriter xmlWriter,
            T o,
            XmlSerializerNamespaces namespaces,
            string encodingStyle,
            string id,
            XmlWriterSettings settings = null)
        {
            using (XmlWriter writer = settings == null ? xmlWriter : XmlWriter.Create(xmlWriter, settings))
                _serializer.Serialize(writer, o, namespaces, encodingStyle, id);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified string.
        /// </summary>
        /// settings are used <see cref="DefaultXmlReaderSettings"/>.</param>
        /// <returns> The deserialized object <typeparamref name="T"/>. </returns>
        public static T Deserialize(string text)
        {
            using (StringReader reader = new StringReader(text))
            using (XmlReader xmlReader = XmlReader.Create(reader))
                return (T)_serializer.Deserialize(xmlReader);

        }

        /// <summary>
        /// Deserializes the XML document contained in the specified string.
        /// </summary>
        /// <param name="text">String containing the XML document.</param>
        /// <param name="settings"> Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlReaderSettings"/>.</param>
        /// <returns> The deserialized object <typeparamref name="T"/>. </returns>
        public static T Deserialize(string text, XmlReaderSettings settings)
        {
            if (settings == null) settings = _defaultReaderSettings;
            using (StringReader reader = new StringReader(text))
            using (XmlReader xmlReader = XmlReader.Create(reader, settings))
                return (T)_serializer.Deserialize(xmlReader);

        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Containing the XML document to deserialize.
        /// </param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        public static T Deserialize(Stream stream)
        {
            return (T)_serializer.Deserialize(stream);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        /// <see cref="Stream" /> Containing the XML document to deserialize.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlReaderSettings"/>.</param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        public static T Deserialize(Stream stream, XmlReaderSettings settings)
        {
            if (settings == null) settings = _defaultReaderSettings;
            using (XmlReader xmlReader = XmlReader.Create(stream, settings))
                return (T)_serializer.Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="TextReader" />.
        /// </summary>
        /// <param name="textReader">
        /// <see cref="TextReader" /> The containing XML document to deserialize.
        /// </param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during deserialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static T Deserialize(TextReader textReader)
        {
            return (T)_serializer.Deserialize(textReader);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="TextReader" />.
        /// </summary>
        /// <param name="textReader">
        /// <see cref="TextReader" /> The containing XML document to deserialize.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// settings are used <see cref="DefaultXmlReaderSettings"/>.</param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during deserialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static T Deserialize(TextReader textReader, XmlReaderSettings settings)
        {
            if (settings == null) settings = _defaultReaderSettings;
            using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
                return (T)_serializer.Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="XmlReader" />.
        /// </summary>
        /// <param name="xmlReader">
        /// <see cref="XmlReader" /> The containing XML document to deserialize.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// current instance settings are used <see cref="XmlReader" />.</param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during deserialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static T Deserialize(XmlReader xmlReader, XmlReaderSettings settings = null)
        {
            using (XmlReader reader = settings == null ? xmlReader : XmlReader.Create(xmlReader, settings))
                return (T)_serializer.Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="XmlReader" /> and allows you to override events that occur during deserialization.
        /// </summary>
        /// <param name="xmlReader">
        /// <see cref="XmlReader" /> The containing document to deserialize.
        /// </param>
        /// <param name="events">
        /// Class instance <see cref="XmlDeserializationEvents" />.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// current instance settings are used <see cref="XmlReader" />.</param>
        /// <returns>
        /// Deserialized object <typeparamref name="T"/>.
        /// </returns>
        public static T Deserialize(XmlReader xmlReader, XmlDeserializationEvents events, XmlReaderSettings settings = null)
        {
            using (XmlReader reader = settings == null ? xmlReader : XmlReader.Create(xmlReader, settings))
                return (T)_serializer.Deserialize(reader, (string)null, events);
        }

        /// <summary>
        /// Deserializes the XML document contained in the specified <see cref="XmlReader" /> and encoding style.
        /// </summary>
        /// <param name="xmlReader">
        /// <see cref="XmlReader" /> The containing XML document to deserialize.
        /// </param>
        /// <param name="encodingStyle">
        /// The encoding style of the serialized XML.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// current instance settings are used <see cref="XmlReader" />.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="InvalidOperationException">
        /// An error occurred during deserialization.
        /// The original exception is accessible using the <see cref="P:System.Exception.InnerException" /> property.
        /// </exception>
        public static T Deserialize(XmlReader xmlReader, string encodingStyle, XmlReaderSettings settings = null)
        {
            using (XmlReader reader = settings == null ? xmlReader : XmlReader.Create(xmlReader, settings))
                return (T)_serializer.Deserialize(reader, encodingStyle);
        }

        /// <summary>
        /// Deserializes the object using the data contained in the specified <see cref="XmlReader" />.
        /// </summary>
        /// <param name="xmlReader">
        /// An instance of the <see cref="XmlReader" /> class used to read the document.
        /// </param>
        /// <param name="encodingStyle">Encoding used.</param>
        /// <param name="events">
        /// Class instance <see cref="XmlDeserializationEvents" />.
        /// </param>
        /// <param name="settings">Settings for the new <see cref="XmlReader" /> instance. If <see langword="null"/> is specified,
        /// current instance settings are used <see cref="XmlReader" />.</param>
        /// <returns>The deserialized object <typeparamref name="T"/>.</returns>
        public static object Deserialize(
            XmlReader xmlReader,
            string encodingStyle,
            XmlDeserializationEvents events,
            XmlReaderSettings settings = null)
        {
            using (XmlReader reader = settings == null ? xmlReader : XmlReader.Create(xmlReader, settings))
                return _serializer.Deserialize(reader, encodingStyle, events);
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="XmlSerializer" /> can deserialize the specified XML document.
        /// </summary>
        /// <param name="xmlReader">
        /// <see cref="XmlReader" /> Pointing to the document to deserialize.
        /// </param>
        /// <returns>
        /// <see langword="true" /> If this <see cref="XmlSerializer" /> can deserialize an object, <see cref="XmlReader" /> indicates; otherwise, <see langword="false" />.
        /// </returns>
        public static bool CanDeserialize(XmlReader xmlReader)
        {
            return _serializer.CanDeserialize(xmlReader);
        }
    }
}
