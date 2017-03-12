// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET46
using System;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// A decryptor that uses the EncryptedXml class in the desktop CLR.
    /// </summary>
    internal sealed class EncryptedXmlDocumentDecryptor : XmlDocumentDecryptor
    {
        private readonly Func<XmlDocument, EncryptedXml> _encryptedXmlFactory;

        internal EncryptedXmlDocumentDecryptor()
            : this(DefaultEncryptedXmlFactory)
        {
        }

        internal EncryptedXmlDocumentDecryptor(Func<XmlDocument, EncryptedXml> encryptedXmlFactory)
        {
            _encryptedXmlFactory = encryptedXmlFactory;
        }

        protected override XmlReader DecryptDocumentAndCreateXmlReader(XmlDocument document)
        {
            // Perform the actual decryption step, updating the XmlDocument in-place.
            var encryptedXml = _encryptedXmlFactory(document);
            encryptedXml.DecryptDocument();

            // Finally, return the new XmlReader from the updated XmlDocument.
            // Error messages based on this XmlReader won't show line numbers,
            // but that's fine since we transformed the document anyway.
            return document.CreateNavigator().ReadSubtree();
        }

        private static EncryptedXml DefaultEncryptedXmlFactory(XmlDocument document)
        {
            return new EncryptedXml(document);
        }
    }
}
#elif NETSTANDARD1_3
#else
#error Target frameworks need to be updated.
#endif
