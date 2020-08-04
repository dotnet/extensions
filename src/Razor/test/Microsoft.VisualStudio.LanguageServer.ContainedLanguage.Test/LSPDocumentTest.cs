// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class LSPDocumentTest
    {
        public LSPDocumentTest()
        {
            Uri = new Uri("C:/path/to/file.razor");
        }

        private Uri Uri { get; }

        [Fact]
        public void TryGetVirtualDocument_NoCSharpDocument_ReturnsFalse()
        {
            // Arrange
            using var lspDocument = new DefaultLSPDocument(Uri, Mock.Of<ITextBuffer>(), new[] { Mock.Of<VirtualDocument>() });

            // Act
            var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(out var virtualDocument);

            // Assert
            Assert.False(result);
            Assert.Null(virtualDocument);
        }

        [Fact]
        public void TryGetVirtualCSharpDocument_CSharpDocument_ReturnsTrue()
        {
            // Arrange
            var testVirtualDocument = new TestVirtualDocument(Uri, Mock.Of<ITextBuffer>());
            using var lspDocument = new DefaultLSPDocument(Uri, Mock.Of<ITextBuffer>(), new[] { Mock.Of<VirtualDocument>(), testVirtualDocument });

            // Act
            var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(out var virtualDocument);

            // Assert
            Assert.True(result);
            Assert.Same(testVirtualDocument, virtualDocument);
        }
    }
}
