// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class BufferedHtmlContentTest
    {
        [Fact]
        public void AppendString_AppendsAString()
        {
            // Arrange
            var content = new BufferedHtmlContent();

            // Act
            content.Append("Hello");

            // Assert
            var result = Assert.Single(content.Entries);
            Assert.IsType<string>(result);
        }

        [Fact]
        public void AppendString_WrittenAsEncoded()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            content.Append("Hello");

            var writer = new StringWriter();

            // Act
            content.WriteTo(writer, new CommonTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello]]", writer.ToString());
        }

        [Fact]
        public void AppendLine_String_NewLineDoesNotGetEncoded()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            content.AppendLine("Hello");

            var writer = new StringWriter();

            // Act
            content.WriteTo(writer, new CommonTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello]]" + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void AppendLine_HtmlContent_NewLineDoesNotGetEncoded()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            content.AppendLine(new TestHtmlContent("hello"));

            var writer = new StringWriter();

            // Act
            content.WriteTo(writer, new CommonTestEncoder());

            // Assert
            Assert.Equal("Written from TestHtmlContent: hello" + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void AppendCharArray_AppendsAsString()
        {
            // Arrange
            var content = new BufferedHtmlContent();

            // Act
            content.Append(new char[] { 'h', 'e', 'l', 'l', 'o' }, 0, 5);

            // Assert
            var result = Assert.Single(content.Entries);
            Assert.IsType<string>(result);
        }

        [Fact]
        public void AppendIHtmlContent_AppendsAsIs()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            var writer = new StringWriter();

            // Act
            content.Append(new TestHtmlContent("Hello"));

            // Assert
            var result = Assert.Single(content.Entries);
            var testHtmlContent = Assert.IsType<TestHtmlContent>(result);
            testHtmlContent.WriteTo(writer, new CommonTestEncoder());
            Assert.Equal("Written from TestHtmlContent: Hello", writer.ToString());
        }

        [Fact]
        public void CanAppendMultipleItems()
        {
            // Arrange
            var content = new BufferedHtmlContent();

            // Act
            content.Append(new TestHtmlContent("hello"));
            content.Append("Test");

            // Assert
            Assert.Equal(2, content.Entries.Count);
            Assert.Equal("Written from TestHtmlContent: hello", content.Entries[0].ToString());
            Assert.Equal("Test", content.Entries[1]);
        }

        [Fact]
        public void Clear_DeletesAllItems()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            content.Append(new TestHtmlContent("hello"));
            content.Append("Test");

            // Act
            content.Clear();

            // Assert
            Assert.Equal(0, content.Entries.Count);
        }

        [Fact]
        public void WriteTo_WritesAllItems()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            var writer = new StringWriter();
            content.Append(new TestHtmlContent("Hello"));
            content.Append("Test");

            // Act
            content.WriteTo(writer, new CommonTestEncoder());

            // Assert
            Assert.Equal(2, content.Entries.Count);
            Assert.Equal("Written from TestHtmlContent: HelloHtmlEncode[[Test]]", writer.ToString());
        }

        // We're purposely avoiding anything here that actually gets encoded. We don't want to take a dependency
        // on the actual encoder implementation.
        [Fact]
        public void ToString_StringifiesAllContents()
        {
            // Arrange
            var content = new BufferedHtmlContent();
            content.Append(new TestHtmlContent("Hello"));
            content.Append(new TestHtmlContent("Test"));

            // Act & Assert
            Assert.Equal(2, content.Entries.Count);
            Assert.Equal("Written from TestHtmlContent: HelloWritten from TestHtmlContent: Test", content.ToString());
        }

        private class TestHtmlContent : IHtmlContent
        {
            private string _content;

            public TestHtmlContent(string content)
            {
                _content = content;
            }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                writer.Write(ToString());
            }

            public override string ToString()
            {
                return "Written from TestHtmlContent: " + _content;
            }
        }
    }
}
