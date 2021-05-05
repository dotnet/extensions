// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorContentTypeChangeListenerTest
    {
        public RazorContentTypeChangeListenerTest()
        {
            NonRazorContentType = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false, MockBehavior.Strict);
            RazorContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName) == true, MockBehavior.Strict);
            RazorBuffer ??= Mock.Of<ITextBuffer>(buffer => buffer.ContentType == RazorContentType && buffer.Properties == new PropertyCollection(), MockBehavior.Strict);
            DisposedRazorBuffer ??= Mock.Of<ITextBuffer>(buffer => buffer.ContentType == NonRazorContentType && buffer.Properties == new PropertyCollection(), MockBehavior.Strict);
            RazorTextDocument = Mock.Of<ITextDocument>(td => td.TextBuffer == RazorBuffer && td.FilePath == "C:/path/to/file.razor", MockBehavior.Strict);
        }

        private IContentType NonRazorContentType { get; }

        private IContentType RazorContentType { get; }

        private ITextBuffer RazorBuffer { get; }

        private ITextDocument RazorTextDocument { get; }

        private ITextBuffer DisposedRazorBuffer { get; }

        [Fact]
        public void RazorBufferCreated_TracksDocument()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object);

            // Act
            listener.RazorBufferCreated(RazorBuffer);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void RazorBufferCreated_RemoteClient_DoesNotTrackDocument()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var featureDetector = Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsRemoteClient() == true, MockBehavior.Strict);
            var listener = CreateListener(lspDocumentManager.Object, featureDetector);

            // Act & Assert
            listener.RazorBufferCreated(RazorBuffer);
        }

        [Fact]
        public void RazorBufferDisposed_Untracks()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object);

            // Act
            listener.RazorBufferDisposed(DisposedRazorBuffer);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Theory]
        [InlineData(FileActionTypes.ContentSavedToDisk)]
        [InlineData(FileActionTypes.ContentLoadedFromDisk)]
        public void TextDocument_FileActionOccurred_NonRenameEvent_Noops(FileActionTypes fileActionType)
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var listener = CreateListener(lspDocumentManager.Object);
            var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, fileActionType);

            // Act & Assert
            listener.TextDocument_FileActionOccurred(RazorTextDocument, args);
        }

        [Fact]
        public void TextDocument_FileActionOccurred_NonTextDocument_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var listener = CreateListener(lspDocumentManager.Object);
            var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

            // Act & Assert
            listener.TextDocument_FileActionOccurred(RazorBuffer, args);
        }

        [Fact]
        public void TextDocument_FileActionOccurred_NoAssociatedBuffer_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var listener = CreateListener(lspDocumentManager.Object);
            var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);
            var textDocument = new Mock<ITextDocument>(MockBehavior.Strict).Object;
            Mock.Get(textDocument).SetupGet(d => d.TextBuffer).Returns(value: null);

            // Act & Assert
            listener.TextDocument_FileActionOccurred(textDocument, args);
        }

        [Fact]
        public void TextDocument_FileActionOccurred_Rename_UntracksAndThenTracks()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            var tracked = false;
            var untracked = false;
            lspDocumentManager.Setup(manager => manager.TrackDocument(It.IsAny<ITextBuffer>()))
                .Callback(() =>
                {
                    Assert.False(tracked);
                    Assert.True(untracked);

                    tracked = true;
                })
                .Verifiable();
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Callback(() =>
                {
                    Assert.False(tracked);
                    Assert.False(untracked);
                    untracked = true;
                })
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object);
            var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

            // Act
            listener.TextDocument_FileActionOccurred(RazorTextDocument, args);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void TextDocument_FileActionOccurred_Rename_UntracksOnlyIfNotRenamedToRazorContentType()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            var fileToContentTypeService = Mock.Of<IFileToContentTypeService>(detector => detector.GetContentTypeForFilePath(It.IsAny<string>()) == NonRazorContentType, MockBehavior.Strict);
            var tracked = false;
            var untracked = false;
            lspDocumentManager.Setup(manager => manager.UntrackDocument(It.IsAny<ITextBuffer>()))
                .Callback(() =>
                {
                    Assert.False(tracked);
                    Assert.False(untracked);
                    untracked = true;
                })
                .Verifiable();
            var listener = CreateListener(lspDocumentManager.Object, fileToContentTypeService: fileToContentTypeService);
            var args = new TextDocumentFileActionEventArgs("C:/path/to/file.razor", DateTime.UtcNow, FileActionTypes.DocumentRenamed);

            // Act
            listener.TextDocument_FileActionOccurred(RazorTextDocument, args);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        private RazorContentTypeChangeListener CreateListener(
            TrackingLSPDocumentManager lspDocumentManager = null,
            LSPEditorFeatureDetector lspEditorFeatureDetector = null,
            IFileToContentTypeService fileToContentTypeService = null)
        {
            var textDocumentFactory = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict).Object;
            Mock.Get(textDocumentFactory).Setup(f => f.TryGetTextDocument(It.IsAny<ITextBuffer>(), out It.Ref<ITextDocument>.IsAny)).Returns(false);

            lspDocumentManager ??= Mock.Of<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspEditorFeatureDetector ??= Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsLSPEditorAvailable(It.IsAny<string>(), null) == true && detector.IsRemoteClient() == false, MockBehavior.Strict);
            fileToContentTypeService ??= Mock.Of<IFileToContentTypeService>(detector => detector.GetContentTypeForFilePath(It.IsAny<string>()) == RazorContentType, MockBehavior.Strict);
            var textManager = new Mock<IVsTextManager2>(MockBehavior.Strict);
            textManager.Setup(m => m.GetUserPreferences2(null, null, It.IsAny<LANGPREFERENCES2[]>(), null)).Returns(VSConstants.E_NOTIMPL);
            var listener = new RazorContentTypeChangeListener(
                textDocumentFactory,
                lspDocumentManager,
                lspEditorFeatureDetector,
                Mock.Of<SVsServiceProvider>(s => s.GetService(It.IsAny<Type>()) == textManager.Object, MockBehavior.Strict),
                Mock.Of<IEditorOptionsFactoryService>(s => s.GetOptions(It.IsAny<ITextBuffer>()) == Mock.Of<IEditorOptions>(MockBehavior.Strict), MockBehavior.Strict),
                fileToContentTypeService);

            return listener;
        }
    }
}
