// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(LSPDocumentSynchronizer))]
    internal class DefaultLSPDocumentSynchronizer : LSPDocumentSynchronizer
    {
        // Internal for testing
        internal TimeSpan _synchronizationTimeout = TimeSpan.FromSeconds(2);
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly ConcurrentDictionary<Uri, DocumentSynchronizingContext> _synchronizingContexts;

        [ImportingConstructor]
        public DefaultLSPDocumentSynchronizer(LSPDocumentManager documentManager, JoinableTaskContext joinableTaskContext)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;

            documentManager.Changed += DocumentManager_Changed;
            _synchronizingContexts = new ConcurrentDictionary<Uri, DocumentSynchronizingContext>();
        }

        public async override Task<bool> TrySynchronizeVirtualDocumentAsync(LSPDocumentSnapshot document, VirtualDocumentSnapshot virtualDocument, CancellationToken cancellationToken)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (virtualDocument is null)
            {
                throw new ArgumentNullException(nameof(virtualDocument));
            }

            if (!document.VirtualDocuments.Contains(virtualDocument))
            {
                throw new InvalidOperationException("Virtual document snapshot must belong to the provided LSP document snapshot.");
            }

            if (document.Version == virtualDocument.HostDocumentSyncVersion)
            {
                // Already synchronized
                return true;
            }

            var synchronizingContext = _synchronizingContexts.AddOrUpdate(
                virtualDocument.Uri,
                (uri) => new DocumentSynchronizingContext(virtualDocument, document.Version, _synchronizationTimeout, cancellationToken),
                (uri, existingContext) =>
                {
                    if (virtualDocument == existingContext.VirtualDocument &&
                        document.Version == existingContext.ExpectedHostDocumentVersion)
                    {
                        // Already contain a synchronizing context that represents this request and it's in-process of being calculated.
                        return existingContext;
                    }

                    // Cancel old request
                    existingContext.SetSynchronized(false);
                    return new DocumentSynchronizingContext(virtualDocument, document.Version, _synchronizationTimeout, cancellationToken);
                });

            var result = await _joinableTaskFactory.RunAsync(() => synchronizingContext.OnSynchronizedAsync);

            _synchronizingContexts.TryRemove(virtualDocument.Uri, out _);

            return result;
        }

        // Internal for testing
        internal void DocumentManager_Changed(object sender, LSPDocumentChangeEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Kind != LSPDocumentChangeKind.VirtualDocumentChanged)
            {
                return;
            }

            var lspDocument = args.New;
            for (var i = 0; i < lspDocument.VirtualDocuments.Count; i++)
            {
                if (!_synchronizingContexts.TryGetValue(lspDocument.VirtualDocuments[i].Uri, out var synchronizingContext))
                {
                    continue;
                }

                if (lspDocument.Version == synchronizingContext.ExpectedHostDocumentVersion)
                {
                    synchronizingContext.SetSynchronized(true);
                }
                else if (lspDocument.Version > synchronizingContext.ExpectedHostDocumentVersion)
                {
                    // The LSP document version has surpassed what the projected document was expecting for a version. No longer able to synchronize.
                    synchronizingContext.SetSynchronized(false);
                }
            }
        }

        private class DocumentSynchronizingContext
        {
            private readonly TaskCompletionSource<bool> _onSynchronizedSource;
            private readonly CancellationTokenSource _cts;
            private bool _synchronizedSet;

            public DocumentSynchronizingContext(
                VirtualDocumentSnapshot virtualDocument,
                int expectedHostDocumentVersion,
                TimeSpan timeout,
                CancellationToken requestCancellationToken)
            {
                VirtualDocument = virtualDocument;
                ExpectedHostDocumentVersion = expectedHostDocumentVersion;
                _onSynchronizedSource = new TaskCompletionSource<bool>();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);

                // This cancellation token is the one passed in from the call-site that needs to synchronize an LSP document with a virtual document.
                // Meaning, if the outer token is cancelled we need to fail to synchronize.
                _cts.Token.Register(() => SetSynchronized(false));
                _cts.CancelAfter(timeout);
            }

            public VirtualDocumentSnapshot VirtualDocument { get; }

            public int ExpectedHostDocumentVersion { get; }

            public Task<bool> OnSynchronizedAsync => _onSynchronizedSource.Task;

            public void SetSynchronized(bool result)
            {
                lock (_onSynchronizedSource)
                {
                    if (_synchronizedSet)
                    {
                        return;
                    }

                    _synchronizedSet = true;
                    _onSynchronizedSource.SetResult(result);
                }
            }
        }
    }
}
