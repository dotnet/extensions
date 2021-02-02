// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class DefaultFileUriProviderTest
    {
        public DefaultFileUriProviderTest()
        {
            TextBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
        }

        private ITextBuffer TextBuffer { get; }

        [Fact]
        public void AddOrUpdate_Adds()
        {
            // Arrange
            var expectedUri = new Uri("C:/path/to/file.razor");
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));

            // Act
            uriProvider.AddOrUpdate(TextBuffer, expectedUri);

            // Assert
            Assert.True(uriProvider.TryGet(TextBuffer, out var uri));
            Assert.Same(expectedUri, uri);
        }

        [Fact]
        public void AddOrUpdate_Updates()
        {
            // Arrange
            var expectedUri = new Uri("C:/path/to/file.razor");
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));
            uriProvider.AddOrUpdate(TextBuffer, new Uri("C:/original/uri.razor"));

            // Act
            uriProvider.AddOrUpdate(TextBuffer, expectedUri);

            // Assert
            Assert.True(uriProvider.TryGet(TextBuffer, out var uri));
            Assert.Same(expectedUri, uri);
        }

        [Fact]
        public void TryGet_Exists_ReturnsTrue()
        {
            // Arrange
            var expectedUri = new Uri("C:/path/to/file.razor");
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));
            uriProvider.AddOrUpdate(TextBuffer, expectedUri);

            // Act
            var result = uriProvider.TryGet(TextBuffer, out var uri);

            // Assert
            Assert.True(result);
            Assert.Same(expectedUri, uri);
        }

        [Fact]
        public void TryGet_DoesNotExist_ReturnsFalse()
        {
            // Arrange
            var uriProvider = new DefaultFileUriProvider(Mock.Of<ITextDocumentFactoryService>(MockBehavior.Strict));

            // Act
            var result = uriProvider.TryGet(TextBuffer, out var uri);

            // Assert
            Assert.False(result);
            Assert.Null(uri);
        }

        [Fact]
        public void GetOrCreate_NoTextDocument_Creates()
        {
            // Arrange
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
            textDocumentFactoryService.Setup(s => s.TryGetTextDocument(TextBuffer, out It.Ref<ITextDocument>.IsAny)).Returns(false);
            var uriProvider = new DefaultFileUriProvider(textDocumentFactoryService.Object);

            // Act
            var uri = uriProvider.GetOrCreate(TextBuffer);

            // Assert
            Assert.NotNull(uri);
        }

        [Fact]
        public void GetOrCreate_NoTextDocument_MemoizesGeneratedUri()
        {
            // Arrange
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
            textDocumentFactoryService.Setup(s => s.TryGetTextDocument(TextBuffer, out It.Ref<ITextDocument>.IsAny)).Returns(false);
            var uriProvider = new DefaultFileUriProvider(textDocumentFactoryService.Object);

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
            var factory = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict);
            var expectedFilePath = "C:/path/to/file.razor";
            var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedFilePath, MockBehavior.Strict);
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
