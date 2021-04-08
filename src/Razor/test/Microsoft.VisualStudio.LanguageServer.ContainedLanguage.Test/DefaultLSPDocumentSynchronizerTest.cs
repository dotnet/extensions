// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class DefaultLSPDocumentSynchronizerTest
    {
        public DefaultLSPDocumentSynchronizerTest()
        {
            var snapshot = new StringTextSnapshot("Hello World");
            var buffer = new TestTextBuffer(snapshot);
            VirtualDocumentTextBuffer = buffer;
            snapshot.TextBuffer = buffer;
            VirtualDocumentSnapshot = snapshot;
        }

        private ITextSnapshot VirtualDocumentSnapshot { get; }

        private ITextBuffer VirtualDocumentTextBuffer { get; }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SynchronizedDocument_ReturnsTrue()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 123, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider);
            NotifyLSPDocumentAdded(lspDocument, synchronizer);
            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, virtualDocument.HostDocumentSyncVersion.Value);

            // Act
            var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SynchronizesAfterUpdate_ReturnsTrue()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
            };
            NotifyLSPDocumentAdded(lspDocument, synchronizer);

            // Act

            // Start synchronization, this will hang until we notify the buffer versions been updated because the above virtual document expects host doc version 123 but the host doc is 124
            var synchronizeTask = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None);

            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, lspDocument.Version);
            var result = await synchronizeTask.ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousEqualSynchronizationRequests_ReturnsTrue()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
            };
            NotifyLSPDocumentAdded(lspDocument, synchronizer);

            // Act

            // Start synchronization, this will hang until we notify the buffer versions been updated because the above virtual document expects host doc version 123 but the host doc is 124
            var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None);
            var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None);

            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, lspDocument.Version);
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
            var (originalLSPDocument, originalVirtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, originalVirtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
            };
            NotifyLSPDocumentAdded(originalLSPDocument, synchronizer);

            // Act

            // Start synchronization, this will hang until we notify the buffer versions been updated because the above virtual document expects host doc version 123 but the host doc is 124
            var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(originalLSPDocument.Version, originalVirtualDocument, CancellationToken.None);

            // Start another synchronization that will also hang because 124 != 125. However, this synchronization request is for the same addressable virtual document (same URI)
            // therefore requesting a second synchronization with a different host doc version expectation will cancel the original synchronization request resulting it returning
            // false.
            var (newLSPDocument, newVirtualDocument) = CreateDocuments(lspDocumentVersion: 125, virtualDocumentSyncVersion: 124);
            var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(newLSPDocument.Version, newVirtualDocument, CancellationToken.None);

            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, newLSPDocument.Version);
            var result1 = await synchronizeTask1.ConfigureAwait(false);
            var result2 = await synchronizeTask2.ConfigureAwait(false);

            // Assert
            Assert.False(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_SimultaneousSynchronizationRequests_PlatformCancelsFirst_ReturnsFalseThenTrue()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
            };
            NotifyLSPDocumentAdded(lspDocument, synchronizer);
            using var cts = new CancellationTokenSource();

            // Act

            // Start synchronization, this will hang until we notify the buffer versions been updated because the above virtual document expects host doc version 123 but the host doc is 124
            var synchronizeTask1 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, cts.Token);
            var synchronizeTask2 = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None);

            cts.Cancel();

            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, lspDocument.Version);
            var result1 = await synchronizeTask1.ConfigureAwait(false);
            var result2 = await synchronizeTask2.ConfigureAwait(false);

            // Assert
            Assert.False(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_DocumentRemoved_CancelsActiveRequests()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 124, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(500)
            };
            NotifyLSPDocumentAdded(lspDocument, synchronizer);

            var synchronizedTask = synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None).ConfigureAwait(false);

            // Act
            NotifyLSPDocumentRemoved(lspDocument, synchronizer);
            NotifyBufferVersionUpdated(VirtualDocumentTextBuffer, lspDocument.Version);

            var result = await synchronizedTask;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TrySynchronizeVirtualDocumentAsync_Timeout_ReturnsFalse()
        {
            // Arrange
            var (lspDocument, virtualDocument) = CreateDocuments(lspDocumentVersion: 123, virtualDocumentSyncVersion: 123);
            var fileUriProvider = CreateUriProviderFor(VirtualDocumentTextBuffer, virtualDocument.Uri);
            var synchronizer = new DefaultLSPDocumentSynchronizer(fileUriProvider)
            {
                _synchronizationTimeout = TimeSpan.FromMilliseconds(10)
            };
            NotifyLSPDocumentAdded(lspDocument, synchronizer);

            // We're not going to notify that the buffer version was updated so the synchronization will wait until a timeout occurs.

            // Act
            var result = await synchronizer.TrySynchronizeVirtualDocumentAsync(lspDocument.Version, virtualDocument, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(result);
        }

        private static void NotifyLSPDocumentAdded(LSPDocumentSnapshot lspDocument, DefaultLSPDocumentSynchronizer synchronizer)
        {
            var args = new LSPDocumentChangeEventArgs(old: null, @new: lspDocument, LSPDocumentChangeKind.Added);
            synchronizer.DocumentManager_Changed(sender: null, args);
        }

        private static void NotifyLSPDocumentRemoved(LSPDocumentSnapshot lspDocument, DefaultLSPDocumentSynchronizer synchronizer)
        {
            var args = new LSPDocumentChangeEventArgs(old: lspDocument, @new: null, LSPDocumentChangeKind.Removed);
            synchronizer.DocumentManager_Changed(sender: null, args);
        }

        private (TestLSPDocumentSnapshot, TestVirtualDocumentSnapshot) CreateDocuments(int lspDocumentVersion, long virtualDocumentSyncVersion)
        {
            var virtualDocumentUri = new Uri("C:/path/to/file.razor__virtual.cs");
            var virtualDocument = new TestVirtualDocumentSnapshot(virtualDocumentUri, virtualDocumentSyncVersion, VirtualDocumentSnapshot);
            var documentUri = new Uri("C:/path/to/file.razor");
            var document = new TestLSPDocumentSnapshot(documentUri, lspDocumentVersion, virtualDocument);

            return (document, virtualDocument);
        }

        private FileUriProvider CreateUriProviderFor(ITextBuffer textBuffer, Uri bufferUri)
        {
            var fileUriProvider = Mock.Of<FileUriProvider>(provider => provider.TryGet(textBuffer, out bufferUri) == true, MockBehavior.Strict);
            return fileUriProvider;
        }

        private static void NotifyBufferVersionUpdated(ITextBuffer textBuffer, long hostDocumentSyncVersion)
        {
            textBuffer.SetHostDocumentSyncVersion(hostDocumentSyncVersion);
            var edit = textBuffer.CreateEdit();

            // Content doesn't matter, we're just trying to create an edit that notifies listeners of a changed event.
            edit.Insert(0, "Test");
            edit.Apply();
        }
    }
}
