// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultLSPDocumentSynchronizerTest
    {
        public DefaultLSPDocumentSynchronizerTest()
        {
            DocumentManager = Mock.Of<LSPDocumentManager>();
            JoinableTaskContext = new JoinableTaskContext();
            LSPDocumentUri = new Uri("C:/path/to/file.razor");
            VirtualDocumentUri = new Uri("C:/path/to/file.razor__virtual.cs");
        }

        private LSPDocumentManager DocumentManager { get; }

        private JoinableTaskContext JoinableTaskContext { get; }

        private Uri LSPDocumentUri { get; }

        private Uri VirtualDocumentUri { get; }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SynchronizedDocument_ReturnsTrue()
        {
            // Arrange
            var synchronizer = new DefaultLSPDocumentSynchronizer(DocumentManager, JoinableTaskContext);
            var virtualDocument = new TestVirtualDocumentSnapshot(VirtualDocumentUri, 123);
            var document = new TestLSPDocumentSnapshot(LSPDocumentUri, 123, virtualDocument);

            // Act
            var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(document, virtualDocument, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SynchronizesAfterUpdate_ReturnsTrue()
        {
            // Arrange
            var synchronizer = new DefaultLSPDocumentSynchronizer(DocumentManager, JoinableTaskContext);
            synchronizer._synchronizationTimeout = TimeSpan.FromMilliseconds(500);
            var originalVirtualDocument = new TestVirtualDocumentSnapshot(VirtualDocumentUri, 123);
            var originalDocument = new TestLSPDocumentSnapshot(LSPDocumentUri, 124, originalVirtualDocument);

            // Start synchronization, this will hang until we invoke a DocumentManager_Changed event because the above virtual document expects host doc version 123 but the host doc is 124
            var synchronizeTask = synchronizer.TrySynchronizeVirtualDocumentAsync(originalDocument, originalVirtualDocument, CancellationToken.None);

            // Create a virtual and host doc that are synchronized (both at version 124).
            var newVirtualDocument = originalVirtualDocument.Fork(124);
            var newDocument = originalDocument.Fork(124, newVirtualDocument);
            var args = new LSPDocumentChangeEventArgs(originalDocument, newDocument, LSPDocumentChangeKind.VirtualDocumentChanged);

            // Act
            synchronizer.DocumentManager_Changed(DocumentManager, args);
            var result = await synchronizeTask.ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousEqualSynchronizationRequests_ReturnsTrue()
        {
            // Arrange
            var synchronizer = new DefaultLSPDocumentSynchronizer(DocumentManager, JoinableTaskContext);
            synchronizer._synchronizationTimeout = TimeSpan.FromMilliseconds(500);
            var originalVirtualDocument = new TestVirtualDocumentSnapshot(VirtualDocumentUri, 123);
            var originalDocument = new TestLSPDocumentSnapshot(LSPDocumentUri, 124, originalVirtualDocument);

            // Start synchronize
            var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(originalDocument, originalVirtualDocument, CancellationToken.None);
            var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(originalDocument, originalVirtualDocument, CancellationToken.None);

            var newVirtualDocument = originalVirtualDocument.Fork(124);
            var newDocument = originalDocument.Fork(124, newVirtualDocument);
            var args = new LSPDocumentChangeEventArgs(originalDocument, newDocument, LSPDocumentChangeKind.VirtualDocumentChanged);

            // Act
            synchronizer.DocumentManager_Changed(DocumentManager, args);
            var result1 = await synchronizeTask1.ConfigureAwait(false);
            var result2 = await synchronizeTask2.ConfigureAwait(false);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousDifferentSynchronizationRequests_CancelsFirst_ReturnsFalseThenTrue()
        {
            // Arrange
            var synchronizer = new DefaultLSPDocumentSynchronizer(DocumentManager, JoinableTaskContext);
            synchronizer._synchronizationTimeout = TimeSpan.FromMilliseconds(500);
            var originalVirtualDocument = new TestVirtualDocumentSnapshot(VirtualDocumentUri, 123);
            var originalDocument = new TestLSPDocumentSnapshot(LSPDocumentUri, 124, originalVirtualDocument);

            // Start synchronization that will hang because 123 != 124
            var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(originalDocument, originalVirtualDocument, CancellationToken.None);

            var newVirtualDocument = originalVirtualDocument.Fork(124);
            var newDocument = originalDocument.Fork(125, newVirtualDocument);

            // Start another synchronization that will also hang because 124 != 125. However, this synchronization request is for the same addressable virtual document (same URI)
            // therefore requesting a second synchronization with a different host doc version expectation will cancel the original synchronization request resulting it returning
            // false.
            var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(newDocument, newVirtualDocument, CancellationToken.None);

            var finalVirtualDocument = newVirtualDocument.Fork(125);
            var finalDocument = newDocument.Fork(125, finalVirtualDocument);

            var args = new LSPDocumentChangeEventArgs(newDocument, finalDocument, LSPDocumentChangeKind.VirtualDocumentChanged);


            // Act
            synchronizer.DocumentManager_Changed(DocumentManager, args);
            var result1 = await synchronizeTask1.ConfigureAwait(false);
            var result2 = await synchronizeTask2.ConfigureAwait(false);

            // Assert
            Assert.False(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_Timeout_ReturnsFalse()
        {
            // Arrange
            var synchronizer = new DefaultLSPDocumentSynchronizer(DocumentManager, JoinableTaskContext);
            synchronizer._synchronizationTimeout = TimeSpan.FromMilliseconds(10);
            var originalVirtualDocument = new TestVirtualDocumentSnapshot(VirtualDocumentUri, 123);
            var originalDocument = new TestLSPDocumentSnapshot(LSPDocumentUri, 124, originalVirtualDocument);

            // Start synchronize
            var synchronizeTask = synchronizer.TrySynchronizeVirtualDocumentAsync(originalDocument, originalVirtualDocument, CancellationToken.None);

            // Act
            var result = await synchronizeTask.ConfigureAwait(false);

            // Assert
            Assert.False(result);
        }
    }
}
