// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class HtmlVirtualDocumentFactoryTest
    {
        public HtmlVirtualDocumentFactoryTest()
        {
            var htmlContentType = Mock.Of<IContentType>();
            ContentTypeRegistry = Mock.Of<IContentTypeRegistryService>(
                registry => registry.GetContentType(RazorLSPConstants.HtmlLSPContentTypeName) == htmlContentType);
            var textBufferFactory = new Mock<ITextBufferFactoryService>();
            textBufferFactory
                .Setup(factory => factory.CreateTextBuffer())
                .Returns(Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>() && buffer.Properties == new PropertyCollection()));
            TextBufferFactory = textBufferFactory.Object;

            var razorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName) == true);
            RazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType);

            var nonRazorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false);
            NonRazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType);

            TextDocumentFactoryService = Mock.Of<ITextDocumentFactoryService>();
        }

        private ITextBuffer NonRazorLSPBuffer { get; }

        private ITextBuffer RazorLSPBuffer { get; }

        private IContentTypeRegistryService ContentTypeRegistry { get; }

        private ITextBufferFactoryService TextBufferFactory { get; }

        private ITextDocumentFactoryService TextDocumentFactoryService { get; }

        [Fact]
        public void TryCreateFor_NonRazorLSPBuffer_ReturnsFalse()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri);
            var factory = new HtmlVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, TextDocumentFactoryService, uriProvider);

            // Act
            var result = factory.TryCreateFor(NonRazorLSPBuffer, out var virtualDocument);

            using (virtualDocument)
            {
                // Assert
                Assert.False(result);
                Assert.Null(virtualDocument);
            }
        }

        [Fact]
        public void TryCreateFor_RazorLSPBuffer_ReturnsHtmlVirtualDocumentAndTrue()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(RazorLSPBuffer) == uri);
            var factory = new HtmlVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, TextDocumentFactoryService, uriProvider);

            // Act
            var result = factory.TryCreateFor(RazorLSPBuffer, out var virtualDocument);

            using (virtualDocument)
            {
                // Assert
                Assert.True(result);
                Assert.NotNull(virtualDocument);
                Assert.EndsWith(RazorLSPConstants.VirtualHtmlFileNameSuffix, virtualDocument.Uri.OriginalString, StringComparison.Ordinal);
            }
        }
    }
}
