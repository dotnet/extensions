// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
// These tests only run on desktop CLR.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel
{
    public partial class XmlConfigurationSourceTest
    {
        [Fact]
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
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath, new EncryptedXmlDocumentDecryptor(doc =>
            {
                var innerEncryptedXml = new EncryptedXml(doc);
                innerEncryptedXml.AddKeyNameMapping("myKey", aes);
                return innerEncryptedXml;
            }));

            // Act
            xmlConfigSrc.Load(StringToStream(xmlDocument.OuterXml));

            // Assert
            Assert.Equal("Test.Connection.String", xmlConfigSrc.Get("DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("DATA.SETTING:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("data.setting:inventory:connectionstring"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data.setting:Inventory:Provider"));
        }
    }
}
#endif
