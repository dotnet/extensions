// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
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
            var virtualDocumentMock = new Mock<VirtualDocument>(MockBehavior.Strict);
            virtualDocumentMock.Setup(d => d.Dispose()).Verifiable();
            using var lspDocument = new DefaultLSPDocument(Uri, Mock.Of<ITextBuffer>(MockBehavior.Strict), new[] { virtualDocumentMock.Object });

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
            var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);
            textBuffer.SetupGet(b => b.CurrentSnapshot).Returns((ITextSnapshot)null);
            textBuffer.Setup(b => b.ChangeContentType(It.IsAny<IContentType>(), null)).Verifiable();
            textBuffer.SetupGet(b => b.Properties).Returns(new PropertyCollection());
            var testVirtualDocument = new TestVirtualDocument(Uri, textBuffer.Object);
            var virtualDocumentMock = new Mock<VirtualDocument>(MockBehavior.Strict);
            virtualDocumentMock.Setup(d => d.Dispose()).Verifiable();
            using var lspDocument = new DefaultLSPDocument(Uri, Mock.Of<ITextBuffer>(MockBehavior.Strict), new[] { virtualDocumentMock.Object, testVirtualDocument });

            // Act
            var result = lspDocument.TryGetVirtualDocument<TestVirtualDocument>(out var virtualDocument);

            // Assert
            Assert.True(result);
            Assert.Same(testVirtualDocument, virtualDocument);
        }
    }
}
