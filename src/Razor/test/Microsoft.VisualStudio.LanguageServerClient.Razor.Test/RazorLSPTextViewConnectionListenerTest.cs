// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorLSPTextViewConnectionListenerTest
    {
        public RazorLSPTextViewConnectionListenerTest()
        {
            var razorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(RazorLSPContentTypeDefinition.Name) == true);
            RazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == razorLSPContentType);

            var nonRazorLSPContentType = Mock.Of<IContentType>(contentType => contentType.IsOfType(It.IsAny<string>()) == false);
            NonRazorLSPBuffer = Mock.Of<ITextBuffer>(textBuffer => textBuffer.ContentType == nonRazorLSPContentType);
        }

        private ITextBuffer NonRazorLSPBuffer { get; }

        private ITextBuffer RazorLSPBuffer { get; }

        [Fact]
        public void SubjectBuffersConnected_NoLSPRazorBuffers_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            var listener = new RazorLSPTextViewConnectionListener(lspDocumentManager.Object);
            var subjectBuffers = new List<ITextBuffer>() { NonRazorLSPBuffer };
            var textView = Mock.Of<ITextView>();

            // Act
            listener.SubjectBuffersConnected(textView, ConnectionReason.TextViewLifetime, subjectBuffers);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void SubjectBuffersConnected_TracksLSPRazorBuffers()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.TrackDocumentView(RazorLSPBuffer, textView))
                .Verifiable();
            var listener = new RazorLSPTextViewConnectionListener(lspDocumentManager.Object);
            var subjectBuffers = new List<ITextBuffer>() { NonRazorLSPBuffer, RazorLSPBuffer };

            // Act
            listener.SubjectBuffersConnected(textView, ConnectionReason.TextViewLifetime, subjectBuffers);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void SubjectBuffersDisconnected_NoLSPRazorBuffers_Noops()
        {
            // Arrange
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            var listener = new RazorLSPTextViewConnectionListener(lspDocumentManager.Object);
            var subjectBuffers = new List<ITextBuffer>() { NonRazorLSPBuffer };
            var textView = Mock.Of<ITextView>();

            // Act
            listener.SubjectBuffersDisconnected(textView, ConnectionReason.TextViewLifetime, subjectBuffers);

            // Assert
            lspDocumentManager.VerifyAll();
        }

        [Fact]
        public void SubjectBuffersDisconnected_UntracksLSPRazorBuffers()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var lspDocumentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            lspDocumentManager.Setup(manager => manager.UntrackDocumentView(RazorLSPBuffer, textView))
                .Verifiable();
            var listener = new RazorLSPTextViewConnectionListener(lspDocumentManager.Object);
            var subjectBuffers = new List<ITextBuffer>() { NonRazorLSPBuffer, RazorLSPBuffer };

            // Act
            listener.SubjectBuffersDisconnected(textView, ConnectionReason.TextViewLifetime, subjectBuffers);

            // Assert
            lspDocumentManager.VerifyAll();
        }
    }
}
