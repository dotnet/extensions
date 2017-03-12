// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// Class responsible for encrypting and decrypting XML.
    /// </summary>
    public class XmlDocumentDecryptor
    {
        /// <summary>
        /// Accesses the singleton decryptor instance.
        /// </summary>
#if NET46
        public static readonly XmlDocumentDecryptor Instance = new EncryptedXmlDocumentDecryptor();
#elif NETSTANDARD1_3
        public static readonly XmlDocumentDecryptor Instance = new XmlDocumentDecryptor();
#else
#error Target frameworks need to be updated.
#endif

        /// <summary>
        /// Initializes a XmlDocumentDecryptor.
        /// </summary>
        // don't create an instance of this directly
        protected XmlDocumentDecryptor()
        {
        }

        private static bool ContainsEncryptedData(XmlDocument document)
        {
            // EncryptedXml will simply decrypt the document in-place without telling
            // us that it did so, so we need to perform a check to see if EncryptedXml
            // will actually do anything. The below check for an encrypted data blob
            // is the same one that EncryptedXml would have performed.
#if NET46
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("enc", "http://www.w3.org/2001/04/xmlenc#");
            return (document.SelectSingleNode("//enc:EncryptedData", namespaceManager) != null);
#elif NETSTANDARD1_3
            var matchingNodes = document.GetElementsByTagName("EncryptedData", "http://www.w3.org/2001/04/xmlenc#");
            return (matchingNodes != null && matchingNodes.Count > 0);
#else
#error Target frameworks need to be updated.
#endif
        }

        /// <summary>
        /// Returns an XmlReader that decrypts data transparently.
        /// </summary>
        public XmlReader CreateDecryptingXmlReader(Stream input, XmlReaderSettings settings)
        {
            // XML-based configurations aren't really all that big, so we can buffer
            // the whole thing in memory while we determine decryption operations.
            var memStream = new MemoryStream();
            input.CopyTo(memStream);
            memStream.Position = 0;

            // First, consume the entire XmlReader as an XmlDocument.
            var document = new XmlDocument();
            using (var reader = XmlReader.Create(memStream, settings))
            {
                document.Load(reader);
            }
            memStream.Position = 0;

            if (ContainsEncryptedData(document))
            {
                return DecryptDocumentAndCreateXmlReader(document);
            }
            else
            {
                // If no decryption would have taken place, return a new fresh reader
                // based on the memory stream (which doesn't need to be disposed).
                return XmlReader.Create(memStream, settings);
            }
        }

        /// <summary>
        /// Override to process encrypted XML.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>An XmlReader which can read the document.</returns>
        protected virtual XmlReader DecryptDocumentAndCreateXmlReader(XmlDocument document)
        {
            // by default we don't know how to process encrypted XML
            throw new PlatformNotSupportedException(Resources.Error_EncryptedXmlNotSupported);
        }
    }
}
