// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !NETCOREAPP1_0
// These tests only run on desktop CLR.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public partial class XmlConfigurationTest
    {
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void LoadKeyValuePairsFromValidEncryptedXml()
        {
            var xml = @"
                <settings>
                    <Data.Setting>
                        <DefaultConnection>
                            <Connection.String>Test.Connection.String</Connection.String>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data.Setting>
                </settings>";

            // This AES key will be used to encrypt the 'Inventory' element
            var aes = Aes.Create();
            aes.KeySize = 128;
            aes.GenerateKey();

            // Perform the encryption
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            var encryptedXml = new EncryptedXml(xmlDocument);
            encryptedXml.AddKeyNameMapping("myKey", aes);
            var elementToEncrypt = (XmlElement)xmlDocument.SelectSingleNode("//Inventory");
            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedXml.Encrypt(elementToEncrypt, "myKey"), content: false);

            // Quick sanity check: the document should no longer contain an 'Inventory' element
            Assert.Null(xmlDocument.SelectSingleNode("//Inventory"));

            // Arrange
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());
            xmlConfigSrc.Decryptor = new EncryptedXmlDocumentDecryptor(doc =>
            {
                var innerEncryptedXml = new EncryptedXml(doc);
                innerEncryptedXml.AddKeyNameMapping("myKey", aes);
                return innerEncryptedXml;
            });

            // Act
            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xmlDocument.OuterXml));

            // Assert
            Assert.Equal("Test.Connection.String", xmlConfigSrc.Get("DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("DATA.SETTING:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("data.setting:inventory:connectionstring"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data.setting:Inventory:Provider"));
        }
    }
}
#endif
