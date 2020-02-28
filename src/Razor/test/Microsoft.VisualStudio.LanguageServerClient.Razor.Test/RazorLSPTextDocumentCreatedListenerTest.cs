// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorLSPTextDocumentCreatedListenerTest
    {
        public RazorLSPTextDocumentCreatedListenerTest()
        {
            RazorContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPContentTypeDefinition.Name) == true);
            UnavailableFeatureDetector = Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsLSPEditorAvailable(It.IsAny<string>(), null) == false);
        }

        private IContentType RazorContentType { get; }

        private LSPEditorFeatureDetector UnavailableFeatureDetector { get; }

        [Fact]
        public void IsRazorLSPTextDocument_NullFilePath_ReturnsFalse()
        {
            // Arrange
            var listener = CreateListener();
            var textDocument = CreateTextDocument(filePath: null);

            // Act
            var result = listener.IsRazorLSPTextDocument(textDocument);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRazorLSPTextDocument_NonRazorFilePath_ReturnsFalse()
        {
            // Arrange
            var listener = CreateListener();
            var textDocument = CreateTextDocument(filePath: "file.cs");

            // Act
            var result = listener.IsRazorLSPTextDocument(textDocument);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(".razor")]
        [InlineData(".cshtml")]
        public void IsRazorLSPTextDocument_RazorFilePath_ReturnsTrue(string extension)
        {
            // Arrange
            var listener = CreateListener();
            var textDocument = CreateTextDocument(filePath: "file" + extension);

            // Act
            var result = listener.IsRazorLSPTextDocument(textDocument);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRazorLSPTextDocument_LSPEditorNotAvailable_ReturnsFalse()
        {
            // Arrange
            var listener = CreateListener(lspEditorFeatureDetector: UnavailableFeatureDetector);
            var textDocument = CreateTextDocument(filePath: "file.razor");

            // Act
            var result = listener.IsRazorLSPTextDocument(textDocument);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TextDocumentFactory_TextDocumentCreated_EditorUnavailable_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            var listener = CreateListener(lspDocumentManager.Object, UnavailableFeatureDetector);
            var textDocument = CreateTextDocument(filePath: "file.razor");
            var args = new TextDocumentEventArgs(textDocument);

            // Act
            listener.TextDocumentFactory_TextDocumentCreated(sender: null, args);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void TextDocumentFactory_TextDocumentCreated_TracksDocument()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object);
            var textDocument = CreateTextDocument(filePath: "file.razor");
            var args = new TextDocumentEventArgs(textDocument);

            // Act
            listener.TextDocumentFactory_TextDocumentCreated(sender: null, args);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void TextDocumentFactory_TextDocumentCreated_UninitializedTextBuffer_InitializesWithClientName()
        {
            // Arrange
            var listener = CreateListener();
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(buffer => buffer.ContentType)
                .Returns(Mock.Of<IContentType>());
            textBuffer.Setup(buffer => buffer.ChangeContentType(RazorContentType, null))
                .Verifiable();
            var textBufferProperties = new PropertyCollection();
            textBuffer.Setup(buffer => buffer.Properties)
                .Returns(textBufferProperties);
            var textDocument = CreateTextDocument(filePath: "file.razor", textBuffer.Object);
            var args = new TextDocumentEventArgs(textDocument);

            // Act
            listener.TextDocumentFactory_TextDocumentCreated(sender: null, args);

            // Assert
            textBuffer.VerifyAll();
            Assert.True(textBufferProperties.TryGetProperty<string>(LanguageClientConstants.ClientNamePropertyKey, out _));
        }

        [Fact]
        public void TextDocumentFactory_TextDocumentCreated_UninitializedTextBuffer_RemoteClient_InitializesWithoutClientName()
        {
            // Arrange
            var featureDetector = Mock.Of<LSPEditorFeatureDetector>(detector =>
                detector.IsLSPEditorAvailable(It.IsAny<string>(), null) == true &&
                detector.IsRemoteClient() == true);
            var listener = CreateListener(lspEditorFeatureDetector: featureDetector);
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(buffer => buffer.ContentType)
                .Returns(Mock.Of<IContentType>());
            var textBufferProperties = new PropertyCollection();
            textBuffer.Setup(buffer => buffer.Properties)
                .Returns(textBufferProperties);
            var textDocument = CreateTextDocument(filePath: "file.razor", textBuffer.Object);
            var args = new TextDocumentEventArgs(textDocument);

            // Act
            listener.TextDocumentFactory_TextDocumentCreated(sender: null, args);

            // Assert
            Assert.False(textBufferProperties.TryGetProperty<string>(LanguageClientConstants.ClientNamePropertyKey, out _));
        }

        [Theory]
        [InlineData(".razor")]
        [InlineData(".cshtml")]
        public void TextDocumentFactory_TextDocumentDisposed_RazorFile_Untracks(string extension)
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object);
            var textDocument = CreateTextDocument(filePath: "file" + extension);
            var args = new TextDocumentEventArgs(textDocument);

            // Act
            listener.TextDocumentFactory_TextDocumentDisposed(sender: null, args);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void TextDocumentFactory_TextDocumentDisposed_NonRazorFile_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var listener = CreateListener(lspDocumentManager.Object);
            var textDocument = CreateTextDocument(filePath: "file.txt");
            var args = new TextDocumentEventArgs(textDocument);

            // Act & Assert
            listener.TextDocumentFactory_TextDocumentDisposed(sender: null, args);
        }

        private RazorLSPTextDocumentCreatedListener CreateListener(
            TrackingLSPDocumentManager lspDocumentManager = null,
            LSPEditorFeatureDetector lspEditorFeatureDetector = null)
        {
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>();
            var contentTypeRegistry = Mock.Of<IContentTypeRegistryService>(registry => registry.GetContentType(RazorLSPContentTypeDefinition.Name) == RazorContentType);

            lspDocumentManager ??= Mock.Of<TrackingLSPDocumentManager>();
            lspEditorFeatureDetector ??= Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsLSPEditorAvailable(It.IsAny<string>(), null) == true);
            var listener = new RazorLSPTextDocumentCreatedListener(
                textDocumentFactory,
                contentTypeRegistry,
                lspDocumentManager,
                lspEditorFeatureDetector);

            return listener;
        }

        private ITextDocument CreateTextDocument(string filePath, ITextBuffer textBuffer = null)
        {
            textBuffer ??= Mock.Of<ITextBuffer>(buffer => buffer.ContentType == RazorContentType);
            var textDocument = Mock.Of<ITextDocument>(document =>
                document.FilePath == filePath &&
                document.TextBuffer == textBuffer);

            return textDocument;
        }
    }
}
