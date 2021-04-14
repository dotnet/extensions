// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
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

                // Currently tracked synchronizing context is not sufficient, need to update a new one.
                var onSynchronizedTask = documentContext.GetSynchronizationTaskAsync(requiredHostDocumentVersion, cancellationToken);
                return onSynchronizedTask;
            }
        }

        private void VirtualDocumentBuffer_PostChanged(object sender, EventArgs e)
        {
            var textBuffer = (ITextBuffer)sender;

            UpdateDocumentContextVersionInternal(textBuffer);
        }

        private void UpdateDocumentContextVersionInternal(ITextBuffer textBuffer)
        {
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

                        if (!_virtualDocumentContexts.TryGetValue(virtualDocument.Uri, out var virtualDocumentContext))
                        {
                            Debug.Fail("Could not locate virtual document context, it should have been added.");
                            continue;
                        }

                        var virtualDocumentTextBuffer = virtualDocument.Snapshot.TextBuffer;
                        virtualDocumentTextBuffer.PostChanged -= VirtualDocumentBuffer_PostChanged;

                        virtualDocumentContext.Dispose();
                        _virtualDocumentContexts.Remove(virtualDocument.Uri);
                    }
                }
                else if (args.Kind == LSPDocumentChangeKind.VirtualDocumentChanged)
                {
                    if (args.VirtualOld.Snapshot.Version == args.VirtualNew.Snapshot.Version)
                    {
                        // UpdateDocumentContextVersionInternal is typically invoked through a buffer notification,
                        //   however in the case where VirtualDocumentBase.Update is called with a zero change edit,
                        //   there won't be such an edit to hook into. Instead, we'll detect that case here and
                        //   update the document context version appropriately.
                        UpdateDocumentContextVersionInternal(args.VirtualNew.Snapshot.TextBuffer);
                    }
                }
            }
        }

        private class DocumentContext : IDisposable
        {
            private readonly TimeSpan _synchronizingTimeout;
            private readonly List<DocumentSynchronizingContext> _synchronizingContexts;

            public DocumentContext(TimeSpan synchronizingTimeout)
            {
                _synchronizingTimeout = synchronizingTimeout;
                _synchronizingContexts = new List<DocumentSynchronizingContext>();
            }

            public long SeenHostDocumentVersion { get; private set; }

            public void UpdateSeenDocumentVersion(long seenDocumentVersion)
            {
                SeenHostDocumentVersion = seenDocumentVersion;

                if (_synchronizingContexts.Count == 0)
                {
                    // No active synchronizing context for this document.
                    return;
                }

                for (var i = _synchronizingContexts.Count - 1; i >= 0; i--)
                {
                    var synchronizingContext = _synchronizingContexts[i];
                    if (SeenHostDocumentVersion == synchronizingContext.RequiredHostDocumentVersion)
                    {
                        // We're now synchronized!

                        synchronizingContext.SetSynchronized(true);
                        _synchronizingContexts.RemoveAt(i);
                    }
                    else if (SeenHostDocumentVersion > synchronizingContext.RequiredHostDocumentVersion)
                    {
                        // The LSP document version has surpassed what the projected document was expecting for a version. No longer able to synchronize.
                        synchronizingContext.SetSynchronized(false);
                        _synchronizingContexts.RemoveAt(i);
                    }
                    else
                    {
                        // Seen host document version is less than the required version, need to wait longer.
                    }
                }
            }

            public Task<bool> GetSynchronizationTaskAsync(int requiredHostDocumentVersion, CancellationToken cancellationToken)
            {
                // Cancel any out-of-date existing synchronizing contexts.

                for (var i = _synchronizingContexts.Count - 1; i >= 0; i--)
                {
                    var context = _synchronizingContexts[i];
                    if (context.RequiredHostDocumentVersion < requiredHostDocumentVersion)
                    {
                        // All of the existing synchronizations that are older than this version are no longer valid.
                        context.SetSynchronized(result: false);
                        _synchronizingContexts.RemoveAt(i);
                    }
                }

                var synchronizingContext = new DocumentSynchronizingContext(requiredHostDocumentVersion, _synchronizingTimeout, cancellationToken);
                _synchronizingContexts.Add(synchronizingContext);
                return synchronizingContext.OnSynchronizedAsync;
            }

            public void Dispose()
            {
                for (var i = _synchronizingContexts.Count - 1; i >= 0; i--)
                {
                    _synchronizingContexts[i].SetSynchronized(result: false);
                }

                _synchronizingContexts.Clear();
            }

            private class DocumentSynchronizingContext
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
                    _onSynchronizedSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                    }

                    _cts.Dispose();
                    _onSynchronizedSource.SetResult(result);
                }
            }
        }
    }
}
