// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultFileUriProviderTest
    {
        [Fact]
        public void GetOrCreate_NoTextDocument_Creates()
        {
            // Arrange
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>());
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var uri = uriProvider.GetOrCreate(textBuffer);

            // Assert
            Assert.NotNull(uri);
        }

        [Fact]
        public void GetOrCreate_TurnsTextDocumentFilePathIntoUri()
        {
            // Arrange
            var factory = new Mock<ITextDocumentFactoryService>();
            var textBuffer = Mock.Of<ITextBuffer>();
            var expectedFilePath = "C:/path/to/file.razor";
            var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedFilePath);
            factory.Setup(f => f.TryGetTextDocument(textBuffer, out textDocument))
                .Returns(true);
            var uriProvider = new DefaultFileUriProvider(factory.Object);

            // Act
            var uri = uriProvider.GetOrCreate(textBuffer);

            // Assert
            Assert.Equal(expectedFilePath, uri.OriginalString);
        }
    }
}
