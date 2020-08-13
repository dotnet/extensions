// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class DefaultLSPDocumentFactoryTest
    {
        [Fact]
        public void Create_BuildsLSPDocumentWithTextBufferURI()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri);
            var factory = new DefaultLSPDocumentFactory(uriProvider, Enumerable.Empty<VirtualDocumentFactory>());

            // Act
            var lspDocument = factory.Create(textBuffer);

            // Assert
            Assert.Same(uri, lspDocument.Uri);
        }

        [Fact]
        public void Create_MultipleFactories_CreatesLSPDocumentWithVirtualDocuments()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var uri = new Uri("C:/path/to/file.razor");
            var uriProvider = Mock.Of<FileUriProvider>(p => p.GetOrCreate(textBuffer) == uri);
            var virtualDocument1 = Mock.Of<VirtualDocument>();
            var factory1 = Mock.Of<VirtualDocumentFactory>(f => f.TryCreateFor(textBuffer, out virtualDocument1) == true);
            var virtualDocument2 = Mock.Of<VirtualDocument>();
            var factory2 = Mock.Of<VirtualDocumentFactory>(f => f.TryCreateFor(textBuffer, out virtualDocument2) == true);
            var factory = new DefaultLSPDocumentFactory(uriProvider, new[] { factory1, factory2 });

            // Act
            var lspDocument = factory.Create(textBuffer);

            // Assert
            Assert.Collection(
                lspDocument.VirtualDocuments,
                virtualDocument => Assert.Same(virtualDocument1, virtualDocument),
                virtualDocument => Assert.Same(virtualDocument2, virtualDocument));
        }
    }
}
