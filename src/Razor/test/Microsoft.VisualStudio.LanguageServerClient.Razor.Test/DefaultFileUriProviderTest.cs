// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultFileUriProviderTest
    {
        public DefaultFileUriProviderTest()
        {
            TextBuffer = Mock.Of<ITextBuffer>(buffer => buffer.Properties == new PropertyCollection());
        }

        private ITextBuffer TextBuffer { get; }

        [Fact]
        public void GetOrCreate_NoTextDocument_Creates()
        {
            // Arrange
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>());

            // Act
            var uri = uriProvider.GetOrCreate(TextBuffer);

            // Assert
            Assert.NotNull(uri);
        }

        [Fact]
        public void GetOrCreate_NoTextDocument_MemoizesGeneratedUri()
        {
            // Arrange
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>());

            // Act
            var uri1 = uriProvider.GetOrCreate(TextBuffer);
            var uri2 = uriProvider.GetOrCreate(TextBuffer);

            // Assert
            Assert.NotNull(uri1);
            Assert.Same(uri1, uri2);
        }

        [Fact]
        public void GetOrCreate_TurnsTextDocumentFilePathIntoUri()
        {
            // Arrange
            var factory = new Mock<ITextDocumentFactoryService>();
            var expectedFilePath = "C:/path/to/file.razor";
            var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedFilePath);
            factory.Setup(f => f.TryGetTextDocument(TextBuffer, out textDocument))
                .Returns(true);
            var uriProvider = new DefaultFileUriProvider(factory.Object);

            // Act
            var uri = uriProvider.GetOrCreate(TextBuffer);

            // Assert
            Assert.Equal(expectedFilePath, uri.OriginalString);
        }
    }
}
