// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
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
            LSPDocumentSnapshot = Mock.Of<LSPDocumentSnapshot>();
            LSPDocument = Mock.Of<LSPDocument>(document =>
                document.Uri == Uri &&
                document.CurrentSnapshot == LSPDocumentSnapshot &&
                document.VirtualDocuments == new[] { new TestVirtualDocument() } &&
                document.UpdateVirtualDocument<TestVirtualDocument>(It.IsAny<IReadOnlyList<TextChange>>(), It.IsAny<long>()) == Mock.Of<LSPDocumentSnapshot>());
            LSPDocumentFactory = Mock.Of<LSPDocumentFactory>(factory => factory.Create(TextBuffer) == LSPDocument);
        }

        public JoinableTaskContext JoinableTaskContext { get; }

        private ITextBuffer TextBuffer { get; }

        private Uri Uri { get; }

        private FileUriProvider UriProvider { get; }

        private LSPDocumentFactory LSPDocumentFactory { get; }

        private LSPDocument LSPDocument { get; }

        private LSPDocumentSnapshot LSPDocumentSnapshot { get; }

        [Fact]
        public void TrackDocument_TriggersDocumentAdded()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            var changedCalled = false;
            manager.Changed += (sender, args) =>
            {
                changedCalled = true;
                Assert.Null(args.Old);
                Assert.Same(LSPDocumentSnapshot, args.New);
                Assert.Equal(LSPDocumentChangeKind.Added, args.Kind);
            };

            // Act
            manager.TrackDocument(TextBuffer);

            // Assert
            Assert.True(changedCalled);
        }

        [Fact]
        public void UntrackDocument_TriggersDocumentRemoved()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocument(TextBuffer);
            var changedCalled = false;
            manager.Changed += (sender, args) =>
            {
                changedCalled = true;
                Assert.Null(args.New);
                Assert.Same(LSPDocumentSnapshot, args.Old);
                Assert.Equal(LSPDocumentChangeKind.Removed, args.Kind);
            };

            // Act
            manager.UntrackDocument(TextBuffer);

            // Assert
            Assert.True(changedCalled);
        }

        [Fact]
        public void UpdateVirtualDocument_Noops_UnknownDocument()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            var changedCalled = false;
            manager.Changed += (sender, args) =>
            {
                changedCalled = true;
            };
            var changes = new[] { new TextChange(new TextSpan(1, 1), string.Empty) };

            // Act
            manager.UpdateVirtualDocument<TestVirtualDocument>(Uri, changes, 123);

            // Assert
            Assert.False(changedCalled);
        }

        [Fact]
        public void UpdateVirtualDocument_Noops_NoChangesSameVersion()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocument(TextBuffer);
            var changedCalled = false;
            manager.Changed += (sender, args) =>
            {
                changedCalled = true;
            };
            var changes = Array.Empty<TextChange>();

            // Act
            manager.UpdateVirtualDocument<TestVirtualDocument>(Uri, changes, 123);

            // Assert
            Assert.False(changedCalled);
        }

        [Fact]
        public void UpdateVirtualDocument_InvokesVirtualDocumentChanged()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocument(TextBuffer);
            var changedCalled = false;
            manager.Changed += (sender, args) =>
            {
                changedCalled = true;
                Assert.Same(LSPDocumentSnapshot, args.Old);
                Assert.NotSame(LSPDocumentSnapshot, args.New);
                Assert.Equal(LSPDocumentChangeKind.VirtualDocumentChanged, args.Kind);
            };
            var changes = new[] { new TextChange(new TextSpan(1, 1), string.Empty) };

            // Act
            manager.UpdateVirtualDocument<TestVirtualDocument>(Uri, changes, 123);

            // Assert
            Assert.True(changedCalled);
        }

        [Fact]
        public void TryGetDocument_TrackedDocument_ReturnsTrue()
        {
            // Arrange
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocument(TextBuffer);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.True(result);
            Assert.Same(LSPDocumentSnapshot, lspDocument);
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
            var manager = new DefaultLSPDocumentManager(JoinableTaskContext, UriProvider, LSPDocumentFactory);
            manager.TrackDocument(TextBuffer);
            manager.UntrackDocument(TextBuffer);

            // Act
            var result = manager.TryGetDocument(Uri, out var lspDocument);

            // Assert
            Assert.False(result);
            Assert.Null(lspDocument);
        }

        private class TestVirtualDocument : VirtualDocument
        {
            public override Uri Uri => throw new NotImplementedException();

            public override ITextBuffer TextBuffer => throw new NotImplementedException();

            public override VirtualDocumentSnapshot CurrentSnapshot => throw new NotImplementedException();

            public override long? HostDocumentSyncVersion => 123;

            public override VirtualDocumentSnapshot Update(IReadOnlyList<TextChange> changes, long hostDocumentVersion)
            {
                throw new NotImplementedException();
            }
        }
    }
}
