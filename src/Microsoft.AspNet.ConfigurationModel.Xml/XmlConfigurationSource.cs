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
                var prefixStack = new List<string>();

                SkipUntilRootElement(reader);

                // We process the root element individually since it doesn't contribute to prefix 
                ProcessAttributes(reader, prefixStack, data, AddNamePrefix);
                ProcessAttributes(reader, prefixStack, data, AddAttributePairs);

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            prefixStack.Add(reader.LocalName);
                            ProcessAttributes(reader, prefixStack, data, AddNamePrefix);
                            ProcessAttributes(reader, prefixStack, data, AddAttributePairs);

                            // If current element is self-closing
                            if (reader.IsEmptyElement)
                            {
                                prefixStack.RemoveAt(prefixStack.Count - 1);
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (prefixStack.Any())
                            {
                                prefixStack.RemoveAt(prefixStack.Count - 1);
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            var key = string.Join(Constants.KeyDelimiter, prefixStack);
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
                            throw new FormatException("Unsupported node type '" + reader.NodeType + "' is found." +
                                GetLineInfo(reader));
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

        private string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            // TODO: exception message localization
            return lineInfo == null ?  string.Empty :
                string.Format(" Line {0}, position {1}.", lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private void ProcessAttributes(XmlReader reader, List<string> prefixStack,
            Dictionary<string, string> data, Action<string, string, List<string>, Dictionary<string, string>> act)
        {
            for (int i = 0; i < reader.AttributeCount; ++i)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    // TODO: exception message localization
                    throw new FormatException("Namespace is not supported in configuration files." +
                        GetLineInfo(reader));
                }

                act(reader.LocalName, reader.Value, prefixStack, data);
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        // The special attribute "Name" only contributes to prefix
        // This method adds a prefix if given key-value pair represents a "Name" attribute
        private static void AddNamePrefix(string attrKey, string attrVal, List<string> prefixStack,
            Dictionary<string, string> data)
        {
            if (!string.Equals(attrKey, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If current element is not root element
            if (prefixStack.Any())
            {
                prefixStack[prefixStack.Count - 1] = prefixStack.Last() + ":" + attrVal;
            }
            else
            {
                prefixStack.Add(attrVal);
            }
        }

        // Common attributes contribute to key-value pairs
        // This method adds a key-value pair if given key-value pair represents a common attribute
        private static void AddAttributePairs(string attrKey, string attrVal, List<string> prefixStack,
            Dictionary<string, string> data)
        {
            if (string.Equals(attrKey, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            prefixStack.Add(attrKey);
            var key = string.Join(Constants.KeyDelimiter, prefixStack);
            data[key] = attrVal;
            prefixStack.RemoveAt(prefixStack.Count - 1);
        }
    }
}
