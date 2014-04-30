using System;
using System.IO;
using Xunit;

using Resources = Microsoft.AspNet.ConfigurationModel.Xml.Resources;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class XmlConfigurationSourceTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadKeyValuePairsFromValidXml()
        {
            var xml = 
                @"<settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["DATA:DEFAULTCONNECTION:CONNECTIONSTRING"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["DATA:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["data:inventory:connectionstring"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void CommonAttributesContributeToKeyValuePairs()
        {
            var xml =
                @"<settings Port='8008'>
                    <Data>
                        <DefaultConnection
                            ConnectionString='TestConnectionString'
                            Provider='SqlClient'/>
                        <Inventory
                            ConnectionString='AnotherTestConnectionString'
                            Provider='MySql'/>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(5, xmlConfigSrc.Data.Count);
            Assert.Equal("8008", xmlConfigSrc.Data["Port"]);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportMixingChildElementsAndAttributes()
        {
            var xml =
                @"<settings Port='8008'>
                    <Data>
                        <DefaultConnection Provider='SqlClient'>
                            <ConnectionString>TestConnectionString</ConnectionString>
                        </DefaultConnection>
                        <Inventory ConnectionString='AnotherTestConnectionString'>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(5, xmlConfigSrc.Data.Count);
            Assert.Equal("8008", xmlConfigSrc.Data["Port"]);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void NameAttributeContributesToPrefix()
        {
            var xml = 
                @"<settings>
                    <Data Name='DefaultConnection'>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </Data>
                    <Data Name='Inventory'>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void NameAttributeInRootElementContributesToPrefix()
        {
            var xml = 
                @"<settings Name='Data'>
                    <DefaultConnection>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </DefaultConnection>
                    <Inventory>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Inventory>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportMixingNameAttributesAndCommonAttributes()
        {
            var xml = 
                @"<settings>
                    <Data Name='DefaultConnection'
                          ConnectionString='TestConnectionString'
                          Provider='SqlClient' />
                    <Data Name='Inventory' ConnectionString='AnotherTestConnectionString'>
                          <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportCDATAAsTextNode()
        {
            var xml = 
                @"<settings>
                    <Data>
                        <Inventory>
                            <Provider><![CDATA[SpecialStringWith<>]]></Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(1, xmlConfigSrc.Data.Count);
            Assert.Equal("SpecialStringWith<>", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var xml = 
                @"<!-- Comments --> <settings>
                    <Data> <!-- Comments -->
                        <DefaultConnection>
                            <ConnectionString><!-- Comments -->TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings><!-- Comments -->";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportAndIgnoreXMLDeclaration()
        {
            var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void SupportAndIgnoreProcessingInstructions()
        {
            var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <?xml-stylesheet type='text/xsl' href='style1.xsl'?>
                    <settings>
                        <?xml-stylesheet type='text/xsl' href='style2.xsl'?>
                        <Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </Data>
                    </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);

            xmlConfigSrc.Load(StringToStream(xml));

            Assert.Equal(4, xmlConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", xmlConfigSrc.Data["Data:DefaultConnection:ConnectionString"]);
            Assert.Equal("SqlClient", xmlConfigSrc.Data["Data:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Data["Data:Inventory:ConnectionString"]);
            Assert.Equal("MySql", xmlConfigSrc.Data["Data:Inventory:Provider"]);
        }

        [Fact]
        public void ThrowExceptionWhenFindDTD()
        {
            var xml =
                @"<!DOCTYPE DefaultConnection[
                    <!ELEMENT DefaultConnection (ConnectionString,Provider)>
                    <!ELEMENT ConnectionString (#PCDATA)>
                    <!ELEMENT Provider (#PCDATA)>
                ]>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            var expectedMsg = "For security reasons DTD is prohibited in this XML document. "
                + "To enable DTD processing set the DtdProcessing property on XmlReaderSettings "
                + "to Parse and pass the settings into XmlReader.Create method.";

            var exception = Assert.Throws<System.Xml.XmlException>(() => xmlConfigSrc.Load(StringToStream(xml)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenFindNamespace()
        {
            var xml =
                @"<settings xmlns:MyNameSpace='http://microsoft.com/wwa/mynamespace'>
                    <MyNameSpace:Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </MyNameSpace:Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_NamespaceIsNotSupported(Resources.FormatMsg_LineInfo(1, 11));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load(StringToStream(xml)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new XmlConfigurationSource(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new XmlConfigurationSource(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
        {
            var xml = 
                @"<settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                    </Data>
                    <Data Name='DefaultConnection' ConnectionString='NewConnectionString'>
                        <Provider>NewProvider</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(ArbitraryFilePath);
            var expectedMsg = Resources.FormatError_KeyIsDuplicated("Data:DefaultConnection:ConnectionString",
                Resources.FormatMsg_LineInfo(8, 52));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load(StringToStream(xml)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        private static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }
    }
}
