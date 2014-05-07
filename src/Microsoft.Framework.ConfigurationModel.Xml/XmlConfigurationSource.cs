// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Resources = Microsoft.Framework.ConfigurationModel.Xml.Resources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class XmlConfigurationSource : BaseConfigurationSource
    {
        private const string NameAttributeKey = "Name";

        public XmlConfigurationSource(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
            }

            Path = PathResolver.ResolveAppRelativePath(path);
        }

        public string Path { get; private set; }

        public override void Load()
        {
            using (var stream = new FileStream(Path, FileMode.Open))
            {
                Load(stream);
            }
        }

        public virtual void Commit()
        {
            // If the config file is not found in given path
            // i.e. we don't have a template to follow when generating contents of new config file
            if (!File.Exists(Path))
            {
                var newConfigFileStream = new FileStream(Path, FileMode.CreateNew);

                try
                {
                    // Generate contents and write it to the newly created config file
                    GenerateNewConfig(newConfigFileStream);
                }
                catch
                {
                    newConfigFileStream.Dispose();

                    // The operation should be atomic because we don't want a corrupted config file
                    // So we roll back if the operation fails
                    if (File.Exists(Path))
                    {
                        File.Delete(Path);
                    }

                    // Rethrow the exception
                    throw;
                }
                finally
                {
                    newConfigFileStream.Dispose();
                }

                return;
            }

            // Because we need to read the original contents while generating new contents, the new contents are
            // cached in memory and used to overwrite original contents after we finish reading the original contents
            using (var cacheStream = new MemoryStream())
            {
                using (var inputStream = new FileStream(Path, FileMode.Open))
                {
                    Commit(inputStream, cacheStream);
                }

                // Use the cached new contents to overwrite original contents
                cacheStream.Seek(0, SeekOrigin.Begin);
                using (var outputStream = new FileStream(Path, FileMode.Truncate))
                {
                    cacheStream.CopyTo(outputStream);
                }
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
                                throw new FormatException(Resources.FormatError_KeyIsDuplicated(key,
                                    GetLineInfo(reader)));
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
                            throw new FormatException(Resources.FormatError_UnsupportedNodeType( reader.NodeType,
                                GetLineInfo(reader)));
                    }
                }
            }

            ReplaceData(data);
        }

        // Use the original file as a template while generating new file contents
        // to make sure the format is consistent and comments are not lost
        internal void Commit(Stream inputStream, Stream outputStream)
        {
            var dataCopy = new Dictionary<string, string>(Data, StringComparer.OrdinalIgnoreCase);

            var writerSettings = new XmlWriterSettings()
                {
                    Indent = false,
                    ConformanceLevel = ConformanceLevel.Auto
                };

            var outputWriter = XmlWriter.Create(outputStream, writerSettings);

            var readerSettings = new XmlReaderSettings()
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreWhitespace = false,
                    IgnoreComments = false,
                    IgnoreProcessingInstructions = false
                };

            using (var inputReader = XmlReader.Create(inputStream, readerSettings))
            {
                var prefixStack = new Stack<string>();

                CopyUntilRootElement(inputReader, outputWriter);

                // We process the root element individually since it doesn't contribute to prefix 
                outputWriter.WriteStartElement(inputReader.LocalName);
                ProcessAttributes(inputReader, prefixStack, dataCopy, AddNamePrefix);
                ProcessAttributes(inputReader, prefixStack, dataCopy, CommitAttributePair, outputWriter);

                while (inputReader.Read())
                {
                    switch (inputReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            prefixStack.Push(inputReader.LocalName);
                            outputWriter.WriteStartElement(inputReader.LocalName);

                            ProcessAttributes(inputReader, prefixStack, dataCopy, AddNamePrefix);
                            ProcessAttributes(inputReader, prefixStack, dataCopy, CommitAttributePair, outputWriter);

                            // If current element is self-closing
                            if (inputReader.IsEmptyElement)
                            {
                                outputWriter.WriteEndElement();
                                prefixStack.Pop();
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (prefixStack.Any())
                            {
                                outputWriter.WriteFullEndElement();
                                prefixStack.Pop();
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            var key = string.Join(Constants.KeyDelimiter, prefixStack.Reverse<string>());

                            if (!dataCopy.ContainsKey(key))
                            {
                                throw new InvalidOperationException(Resources.FormatError_CommitWhenNewKeyFound(key));
                            }

                            if (inputReader.NodeType == XmlNodeType.CDATA)
                            {
                                outputWriter.WriteCData(dataCopy[key]);
                            }
                            else
                            {
                                outputWriter.WriteValue(dataCopy[key]);
                            }
                            dataCopy.Remove(key);
                            break;

                        case XmlNodeType.ProcessingInstruction:
                            outputWriter.WriteProcessingInstruction(inputReader.LocalName, inputReader.Value);
                            break;

                        case XmlNodeType.Comment:
                            outputWriter.WriteComment(inputReader.Value);
                            break;

                        case XmlNodeType.Whitespace:
                            outputWriter.WriteWhitespace(inputReader.Value);
                            break;

                        default:
                            throw new FormatException(Resources.FormatError_UnsupportedNodeType(inputReader.NodeType,
                                GetLineInfo(inputReader)));
                    }
                }

                // Close the root element
                outputWriter.WriteEndElement();
                outputWriter.Flush();
            }

            if (dataCopy.Any())
            {
                var missingKeys = string.Join(", ", dataCopy.Keys);
                throw new InvalidOperationException(Resources.FormatError_CommitWhenKeyMissing(missingKeys));
            }
        }

        // Write the contents of newly created config file to given stream
        internal void GenerateNewConfig(Stream outputStream)
        {
            var writerSettings = new XmlWriterSettings()
                {
                    Indent = true,
                    ConformanceLevel = ConformanceLevel.Document
                };

            var outputWriter = XmlWriter.Create(outputStream, writerSettings);

            // The root element of an XML config file can have arbitrary name
            outputWriter.WriteStartElement("settings");

            foreach (var entry in Data)
            {
                var separator = entry.Key.IndexOf(Constants.KeyDelimiter);
                if (separator < 0)
                {
                    // For key-value pairs like "A = B", we generate
                    // <A> B </A>
                    outputWriter.WriteElementString(entry.Key, entry.Value);
                }
                else
                {
                    // For key value pairs like "A:B:C = D", we generate
                    // <A Name="B:C"> D </A>
                    outputWriter.WriteStartElement(entry.Key.Substring(0, separator));
                    outputWriter.WriteAttributeString(NameAttributeKey, entry.Key.Substring(separator + 1));
                    outputWriter.WriteValue(entry.Value);
                    outputWriter.WriteEndElement();
                }
            }

            outputWriter.WriteEndElement();
            outputWriter.Flush();
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

        private void CopyUntilRootElement(XmlReader reader, XmlWriter writer)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(reader.LocalName, reader.Value);
                        break;

                    case XmlNodeType.Comment:
                        writer.WriteComment(reader.Value);
                        break;

                    case XmlNodeType.Whitespace:
                        writer.WriteWhitespace(reader.Value);
                        break;

                    case XmlNodeType.Element:
                        return;

                    default:
                        break;
                }
            }
        }

        private static string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo == null ?  string.Empty :
                Resources.FormatMsg_LineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private void ProcessAttributes(XmlReader reader, Stack<string> prefixStack, IDictionary<string, string> data,
            Action<XmlReader, Stack<string>, IDictionary<string, string>, XmlWriter> act, XmlWriter writer = null)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException(Resources.FormatError_NamespaceIsNotSupported(GetLineInfo(reader)));
                }

                act(reader, prefixStack, data, writer);
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        // The special attribute "Name" only contributes to prefix
        // This method adds a prefix if current node in reader represents a "Name" attribute
        private static void AddNamePrefix(XmlReader reader, Stack<string> prefixStack,
            IDictionary<string, string> data, XmlWriter writer)
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
            IDictionary<string, string> data, XmlWriter writer)
        {
            if (string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            prefixStack.Push(reader.LocalName);
            var key = string.Join(Constants.KeyDelimiter, prefixStack.Reverse<string>());

            if (data.ContainsKey(key))
            {
                throw new FormatException(Resources.FormatError_KeyIsDuplicated(key, GetLineInfo(reader)));
            }

            data[key] = reader.Value;
            prefixStack.Pop();
        }

        // Read an attribute key-value pair and write it to output destination
        // When writting, the value in memory (i.e. data) is used as new value of current attribute
        private static void CommitAttributePair(XmlReader reader, Stack<string> prefixStack,
            IDictionary<string, string> data, XmlWriter writer)
        {
            if (string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteAttributeString(reader.LocalName, reader.Value);
                return;
            }

            prefixStack.Push(reader.LocalName);
            var key = string.Join(Constants.KeyDelimiter, prefixStack.Reverse<string>());

            if (!data.ContainsKey(key))
            {
                throw new InvalidOperationException(Resources.FormatError_CommitWhenNewKeyFound(key));
            }

            writer.WriteAttributeString(reader.LocalName, data[key]);
            data.Remove(key);
            prefixStack.Pop();
        }
    }
}
