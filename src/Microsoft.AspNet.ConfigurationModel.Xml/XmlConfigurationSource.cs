using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class XmlConfigurationSource : BaseConfigurationSource
    {
        private const string NameAttributeKey = "Name";

        public XmlConfigurationSource(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // TODO: exception message localization
                throw new ArgumentException("File path must be a non-empty string", "path");
            }

            Path = path;
        }

        public string Path { get; private set; }

        public override void Load()
        {
            using (var stream = new FileStream(Path, FileMode.Open))
            {
                Load(stream);
            }
        }

        internal void Load(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var readerSettings = new XmlReaderSettings()
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                };

            using (var reader = XmlReader.Create(stream, readerSettings))
            {
                var prefixStack = new Stack<string>();

                SkipUntilRootElement(reader);

                // We process the root element individually since it doesn't contribute to prefix 
                ProcessAttributes(reader, prefixStack, data, AddNamePrefix);
                ProcessAttributes(reader, prefixStack, data, AddAttributePair);

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            prefixStack.Push(reader.LocalName);
                            ProcessAttributes(reader, prefixStack, data, AddNamePrefix);
                            ProcessAttributes(reader, prefixStack, data, AddAttributePair);

                            // If current element is self-closing
                            if (reader.IsEmptyElement)
                            {
                                prefixStack.Pop();
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (prefixStack.Any())
                            {
                                prefixStack.Pop();
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            var key = string.Join(Constants.KeyDelimiter, prefixStack.Reverse<string>());

                            if (data.ContainsKey(key))
                            {
                                throw new FormatException(string.Format("Key '{0}' is duplicated.{1}",
                                    key, GetLineInfo(reader)));
                            }

                            data[key] = reader.Value;
                            break;

                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                            // Ignore certain types of nodes
                            break;

                        default:
                            // TODO: exception message localization
                            throw new FormatException(string.Format("Unsupported node type '{0}' is found.{1}",
                                reader.NodeType, GetLineInfo(reader)));
                    }
                }
            }

            ReplaceData(data);
        }

        private void SkipUntilRootElement(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.XmlDeclaration &&
                    reader.NodeType != XmlNodeType.ProcessingInstruction)
                {
                    break;
                }
            }
        }

        private static string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            // TODO: exception message localization
            return lineInfo == null ?  string.Empty :
                string.Format(" Line {0}, position {1}.", lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private void ProcessAttributes(XmlReader reader, Stack<string> prefixStack,
            Dictionary<string, string> data, Action<XmlReader, Stack<string>, Dictionary<string, string>> act)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    // TODO: exception message localization
                    throw new FormatException(string.Format("Namespace is not supported in configuration files.{0}",
                        GetLineInfo(reader)));
                }

                act(reader, prefixStack, data);
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        // The special attribute "Name" only contributes to prefix
        // This method adds a prefix if current node in reader represents a "Name" attribute
        private static void AddNamePrefix(XmlReader reader, Stack<string> prefixStack, Dictionary<string, string> data)
        {
            if (!string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If current element is not root element
            if (prefixStack.Any())
            {
                var lastPrefix = prefixStack.Pop();
                prefixStack.Push(lastPrefix + Constants.KeyDelimiter + reader.Value);
            }
            else
            {
                prefixStack.Push(reader.Value);
            }
        }

        // Common attributes contribute to key-value pairs
        // This method adds a key-value pair if current node in reader represents a common attribute
        private static void AddAttributePair(XmlReader reader, Stack<string> prefixStack,
            Dictionary<string, string> data)
        {
            if (string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            prefixStack.Push(reader.LocalName);
            var key = string.Join(Constants.KeyDelimiter, prefixStack.Reverse<string>());

            if (data.ContainsKey(key))
            {
                throw new FormatException(string.Format("Key '{0}' is duplicated.{1}", key, GetLineInfo(reader)));
            }

            data[key] = reader.Value;
            prefixStack.Pop();
        }
    }
}
