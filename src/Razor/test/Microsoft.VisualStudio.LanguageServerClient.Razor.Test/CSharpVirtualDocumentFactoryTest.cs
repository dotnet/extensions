// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class CSharpVirtualDocumentFactoryTest
    {
        public CSharpVirtualDocumentFactoryTest()
        {
            var csharpContentType = Mock.Of<IContentType>();
            ContentTypeRegistry = Mock.Of<IContentTypeRegistryService>(
                registry => registry.GetContentType(CSharpVirtualDocumentFactory.CSharpLSPContentTypeName) == csharpContentType);
            TextBufferFactory = Mock.Of<ITextBufferFactoryService>(factory => factory.CreateTextBuffer(csharpContentType) == Mock.Of<ITextBuffer>());

            var razorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPContentTypeDefinition.Name) == true);
            RazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType);

            var nonRazorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false);
            NonRazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType);
        }

        private ITextBuffer NonRazorLSPBuffer { get; }

        private ITextBuffer RazorLSPBuffer { get; }

        private IContentTypeRegistryService ContentTypeRegistry { get; }

        private ITextBufferFactoryService TextBufferFactory { get; }

        [Fact]
        public void TryCreateFor_NonRazorLSPBuffer_ReturnsFalse()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(It.IsAny<ITextBuffer>()) == uri);
            var factory = new CSharpVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, uriProvider);

            // Act
            var result = factory.TryCreateFor(NonRazorLSPBuffer, out var virtualDocument);

            // Assert
            Assert.False(result);
            Assert.Null(virtualDocument);
        }

        [Fact]
        public void TryCreateFor_RazorLSPBuffer_ReturnsCSharpVirtualDocumentAndTrue()
        {
            // Arrange
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(RazorLSPBuffer) == uri);
            var factory = new CSharpVirtualDocumentFactory(ContentTypeRegistry, TextBufferFactory, uriProvider);

            // Act
            var result = factory.TryCreateFor(RazorLSPBuffer, out var virtualDocument);

            // Assert
            Assert.True(result);
            Assert.NotNull(virtualDocument);
            Assert.EndsWith(CSharpVirtualDocumentFactory.VirtualCSharpFileNameSuffix, virtualDocument.Uri.OriginalString);
        }
    }
}
