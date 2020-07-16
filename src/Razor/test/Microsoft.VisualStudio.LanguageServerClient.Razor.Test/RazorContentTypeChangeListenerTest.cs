// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.Client;
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
            NonRazorContentType = Mock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);
            RazorContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName) == true);
            RazorBuffer ??= Mock.Of<ITextBuffer>(buffer => buffer.ContentType == RazorContentType && buffer.Properties == new PropertyCollection());
            DisposedRazorBuffer ??= Mock.Of<ITextBuffer>(buffer => buffer.ContentType == NonRazorContentType && buffer.Properties == new PropertyCollection());
        }

        private IContentType NonRazorContentType { get; }

        private IContentType RazorContentType { get; }

        private ITextBuffer RazorBuffer { get; }

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
            var featureDetector = Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsRemoteClient() == true);
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

        private RazorContentTypeChangeListener CreateListener(
            TrackingLSPDocumentManager lspDocumentManager = null,
            LSPEditorFeatureDetector lspEditorFeatureDetector = null)
        {
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>();

            lspDocumentManager ??= Mock.Of<TrackingLSPDocumentManager>();
            lspEditorFeatureDetector ??= Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsLSPEditorAvailable(It.IsAny<string>(), null) == true);
            var listener = new RazorContentTypeChangeListener(
                textDocumentFactory,
                lspDocumentManager,
                lspEditorFeatureDetector,
                Mock.Of<SVsServiceProvider>(s => s.GetService(It.IsAny<Type>()) == Mock.Of<IVsTextManager2>()),
                Mock.Of<IEditorOptionsFactoryService>(s => s.GetOptions(It.IsAny<ITextBuffer>()) == Mock.Of<IEditorOptions>()));

            return listener;
        }
    }
}
