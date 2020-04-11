// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(LSPDocumentManagerChangeTrigger))]
    [Export(typeof(LSPDocumentSynchronizer))]
    internal class DefaultLSPDocumentSynchronizer : LSPDocumentSynchronizer
    {
        // Internal for testing
        internal TimeSpan _synchronizationTimeout = TimeSpan.FromSeconds(2);
        private readonly Dictionary<Uri, DocumentContext> _virtualDocumentContexts;
        private readonly object DocumentContextLock = new object();
        private readonly FileUriProvider _fileUriProvider;

        [ImportingConstructor]
        public DefaultLSPDocumentSynchronizer(FileUriProvider fileUriProvider)
        {
            if (fileUriProvider is null)
            {
                throw new ArgumentNullException(nameof(fileUriProvider));
            }

            _fileUriProvider = fileUriProvider;
            _virtualDocumentContexts = new Dictionary<Uri, DocumentContext>();
        }

        public override void Initialize(LSPDocumentManager documentManager)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            documentManager.Changed += DocumentManager_Changed;
        }

        public override Task<bool> TrySynchronizeVirtualDocumentAsync(int requiredHostDocumentVersion, VirtualDocumentSnapshot virtualDocument, CancellationToken cancellationToken)
        {
            if (virtualDocument is null)
            {
                throw new ArgumentNullException(nameof(virtualDocument));
            }

            lock (DocumentContextLock)
            {
                if (!_virtualDocumentContexts.TryGetValue(virtualDocument.Uri, out var documentContext))
                {
                    throw new InvalidOperationException("Document context should never be null here.");
                }

                if (requiredHostDocumentVersion == documentContext.SeenHostDocumentVersion)
                {
                    // Already synchronized
                    return Task.FromResult(true);
                }

                if (requiredHostDocumentVersion != documentContext.SynchronizingContext?.RequiredHostDocumentVersion)
                {
                    // Currently tracked synchronizing context is not sufficient, need to update a new one.
                    documentContext.UpdateSynchronizingContext(requiredHostDocumentVersion, cancellationToken);
                }
                else
                {
                    // Already have a synchronizing context for this type of request, memoize the results.
                }

                var synchronizingContext = documentContext.SynchronizingContext;
                return synchronizingContext.OnSynchronizedAsync;
            }
        }

        private void VirtualDocumentBuffer_PostChanged(object sender, EventArgs e)
        {
            var textBuffer = (ITextBuffer)sender;
            if (!_fileUriProvider.TryGet(textBuffer, out var virtualDocumentUri))
            {
                return;
            }

            lock (DocumentContextLock)
            {
                if (!_virtualDocumentContexts.TryGetValue(virtualDocumentUri, out var documentContext))
                {
                    return;
                }

                if (!textBuffer.TryGetHostDocumentSyncVersion(out var hostDocumentVersion))
                {
                    return;
                }

                documentContext.UpdateSeenDocumentVersion(hostDocumentVersion);

                var synchronizingContext = documentContext.SynchronizingContext;
                if (synchronizingContext == null)
                {
                    // No active synchronizing context for this document.
                    return;
                }

                if (documentContext.SeenHostDocumentVersion == synchronizingContext.RequiredHostDocumentVersion)
                {
                    // The buffers host document version indicates that it's now "synchronized". If we were to re-request the VirtualDocumentSnapshot
                    // from the LSPDocumentManager we would see a synchronized LSPDocument. Instead of re-requesting we're making the assumption that
                    // whenever a VirtualDocumentSnapshot is updated it also updates its ITextBuffer sync version.
                    synchronizingContext.SetSynchronized(true);
                }
                else if (documentContext.SeenHostDocumentVersion > synchronizingContext.RequiredHostDocumentVersion)
                {
                    // The LSP document version has surpassed what the projected document was expecting for a version. No longer able to synchronize.
                    synchronizingContext.SetSynchronized(false);
                }
                else
                {
                    // Seen host document version is less than the required version, need to wait longer.
                }
            }
        }

        // Internal for testing
        internal void DocumentManager_Changed(object sender, LSPDocumentChangeEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            lock (DocumentContextLock)
            {
                if (args.Kind == LSPDocumentChangeKind.Added)
                {
                    var lspDocument = args.New;
                    for (var i = 0; i < lspDocument.VirtualDocuments.Count; i++)
                    {
                        var virtualDocument = lspDocument.VirtualDocuments[i];

                        Debug.Assert(!_virtualDocumentContexts.ContainsKey(virtualDocument.Uri));

                        var virtualDocumentTextBuffer = virtualDocument.Snapshot.TextBuffer;
                        virtualDocumentTextBuffer.PostChanged += VirtualDocumentBuffer_PostChanged;
                        _virtualDocumentContexts[virtualDocument.Uri] = new DocumentContext(_synchronizationTimeout);
                    }
                }
                else if (args.Kind == LSPDocumentChangeKind.Removed)
                {
                    var lspDocument = args.Old;
                    for (var i = 0; i < lspDocument.VirtualDocuments.Count; i++)
                    {
                        var virtualDocument = lspDocument.VirtualDocuments[i];

                        Debug.Assert(_virtualDocumentContexts.ContainsKey(virtualDocument.Uri));

                        var virtualDocumentTextBuffer = virtualDocument.Snapshot.TextBuffer;
                        virtualDocumentTextBuffer.PostChanged -= VirtualDocumentBuffer_PostChanged;
                        _virtualDocumentContexts.Remove(virtualDocument.Uri);
                    }
                }
            }
        }

        private class DocumentContext
        {
            private readonly TimeSpan _synchronizingTimeout;

            public DocumentContext(TimeSpan synchronizingTimeout)
            {
                _synchronizingTimeout = synchronizingTimeout;
            }

            public long SeenHostDocumentVersion { get; private set; }

            public DocumentSynchronizingContext SynchronizingContext { get; private set; }

            public void UpdateSeenDocumentVersion(long seenDocumentVersion)
            {
                SeenHostDocumentVersion = seenDocumentVersion;
            }

            public void UpdateSynchronizingContext(int requiredHostDocumentVersion, CancellationToken cancellationToken)
            {
                // Cancel our existing synchronizing context.
                SynchronizingContext?.SetSynchronized(result: false);
                SynchronizingContext = new DocumentSynchronizingContext(requiredHostDocumentVersion, _synchronizingTimeout, cancellationToken);
            }
        }

        public class DocumentSynchronizingContext
        {
            private readonly TaskCompletionSource<bool> _onSynchronizedSource;
            private readonly CancellationTokenSource _cts;
            private bool _synchronizedSet;

            public DocumentSynchronizingContext(
                int requiredHostDocumentVersion,
                TimeSpan timeout,
                CancellationToken requestCancellationToken)
            {
                RequiredHostDocumentVersion = requiredHostDocumentVersion;
                _onSynchronizedSource = new TaskCompletionSource<bool>();
                _cts = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);

                // This cancellation token is the one passed in from the call-site that needs to synchronize an LSP document with a virtual document.
                // Meaning, if the outer token is cancelled we need to fail to synchronize.
                _cts.Token.Register(() => SetSynchronized(false));
                _cts.CancelAfter(timeout);
            }

            public int RequiredHostDocumentVersion { get; }

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
