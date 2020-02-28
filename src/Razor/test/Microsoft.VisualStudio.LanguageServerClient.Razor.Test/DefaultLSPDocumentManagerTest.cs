// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultLSPDocumentManagerTest
    {
        public DefaultLSPDocumentManagerTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskContext = joinableTaskContext.Context;
            TextBuffer = Mock.Of<ITextBuffer>();
            Uri = new Uri("C:/path/to/file.razor");
            UriProvider = Mock.Of<FileUriProvider>(provider => provider.GetOrCreate(TextBuffer) == Uri);
            LSPDocument = Mock.Of<LSPDocument>(document => document.Uri == Uri);
            LSPDocumentFactory = Mock.Of<LSPDocumentFactory>(factory => factory.Create(TextBuffer) == LSPDocument);
        }

        public JoinableTaskContext JoinableTaskContext { get; }

        private ITextBuffer TextBuffer { get; }

        private Uri Uri { get; }

        private FileUriProvider UriProvider { get; }

        private LSPDocumentFactory LSPDocumentFactory { get; }

        public LSPDocument LSPDocument { get; }

        [Fact]
        public void TryGetDocument_TrackedDocument_ReturnsTrue()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocumentView(TextBuffer, textView);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.True(result);
            Assert.Same(LSPDocument, lspDocument);
        }

        [Fact]
        public void TryGetDocument_UnknownDocument_ReturnsFalse()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.False(result);
            Assert.Null(lspDocument);
        }

        [Fact]
        public void TryGetDocument_UntrackedDocument_ReturnsFalse()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocumentView(TextBuffer, textView);
            manager.UntrackDocumentView(TextBuffer, textView);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.False(result);
            Assert.Null(lspDocument);
        }

        [Fact]
        public void TryGetDocument_TrackDocumentMultipleViews_ReturnsTrue()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocumentView(TextBuffer, textView1);
            manager.TrackDocumentView(TextBuffer, textView2);
            manager.UntrackDocumentView(TextBuffer, textView1);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.True(result);
            Assert.Same(LSPDocument, lspDocument);
        }
    }
}
