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
            var htmlContentType = Mock.Of<IContentType>(MockBehavior.Strict);
            ContentTypeRegistry = Mock.Of<IContentTypeRegistryService>(
                registry => registry.GetContentType(RazorLSPConstants.HtmlLSPContentTypeName) == htmlContentType, MockBehavior.Strict);
            var textBufferFactory = new Mock<ITextBufferFactoryService>(MockBehavior.Strict);
            var factoryBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>(MockBehavior.Strict) && buffer.Properties == new PropertyCollection(), MockBehavior.Strict);
            Mock.Get(factoryBuffer).Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), It.IsAny<object>())).Verifiable();
            textBufferFactory
                .Setup(factory => factory.CreateTextBuffer())
                .Returns(factoryBuffer);
            TextBufferFactory = textBufferFactory.Object;

            var razorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName) == true, MockBehavior.Strict);
            RazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType, MockBehavior.Strict);

            var nonRazorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false, MockBehavior.Strict);
            NonRazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType, MockBehavior.Strict);

            TextDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
            Mock.Get(TextDocumentFactoryService).Setup(s => s.CreateTextDocument(It.IsAny<ITextBuffer>(), It.IsAny<string>())).Returns((ITextDocument)null);
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
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri, MockBehavior.Strict);
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
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(RazorLSPBuffer) == uri, MockBehavior.Strict);
            Mock.Get(uriProvider).Setup(p => p.AddOrUpdate(It.IsAny<ITextBuffer>(), It.IsAny<Uri>())).Verifiable();
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
