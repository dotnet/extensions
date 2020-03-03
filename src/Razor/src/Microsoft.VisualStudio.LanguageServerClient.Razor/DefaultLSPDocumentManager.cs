// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(LSPDocumentManager))]
    internal class DefaultLSPDocumentManager : TrackingLSPDocumentManager
    {
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentFactory _documentFactory;
        private readonly Dictionary<Uri, LSPDocument> _documents;

        public override event EventHandler<LSPDocumentChangeEventArgs> Changed;

        [ImportingConstructor]
        public DefaultLSPDocumentManager(
            JoinableTaskContext joinableTaskContext,
            FileUriProvider fileUriProvider,
            LSPDocumentFactory documentFactory)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (fileUriProvider is null)
            {
                throw new ArgumentNullException(nameof(fileUriProvider));
            }

            if (documentFactory is null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _joinableTaskContext = joinableTaskContext;
            _fileUriProvider = fileUriProvider;
            _documentFactory = documentFactory;
            _documents = new Dictionary<Uri, LSPDocument>();
        }

        public override void TrackDocument(ITextBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            var uri = _fileUriProvider.GetOrCreate(buffer);
            if (_documents.TryGetValue(uri, out _))
            {
                throw new InvalidOperationException($"Can not track document that's already being tracked {uri}");
            }

            var lspDocument = _documentFactory.Create(buffer);
            _documents[uri] = lspDocument;
            var args = new LSPDocumentChangeEventArgs(old: null, lspDocument.CurrentSnapshot, LSPDocumentChangeKind.Added);
            Changed?.Invoke(this, args);
        }

        public override void UntrackDocument(ITextBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            var uri = _fileUriProvider.GetOrCreate(buffer);
            if (!_documents.TryGetValue(uri, out var lspDocument))
            {
                // We don't know about this document, noop.
                return;
            }

            _documents.Remove(uri);

            var args = new LSPDocumentChangeEventArgs(lspDocument.CurrentSnapshot, @new: null, LSPDocumentChangeKind.Removed);
            Changed?.Invoke(this, args);
        }

        public override void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<TextChange> changes,
            long hostDocumentVersion)
        {
            if (hostDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(hostDocumentUri));
            }

            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            if (!_documents.TryGetValue(hostDocumentUri, out var lspDocument))
            {
                // Don't know about document, noop.
                return;
            }

            if (changes.Count == 0 &&
                lspDocument.TryGetVirtualDocument<TVirtualDocument>(out var virtualDocument) &&
                virtualDocument.HostDocumentSyncVersion == hostDocumentVersion)
            {
                // The current virtual document already knows about this update. Ignore it so we don't prematurely invoke a change event.
                return;
            }

            var old = lspDocument.CurrentSnapshot;
            var @new = lspDocument.UpdateVirtualDocument<TVirtualDocument>(changes, hostDocumentVersion);

            if (old == @new)
            {
                return;
            }

            var args = new LSPDocumentChangeEventArgs(old, @new, LSPDocumentChangeKind.VirtualDocumentChanged);
            Changed?.Invoke(this, args);
        }

        public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
        {
            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            if (!_documents.TryGetValue(uri, out var lspDocument))
            {
                // This should never happen in practice but return `null` so our tests can validate
                lspDocumentSnapshot = null;
                return false;
            }

            lspDocumentSnapshot = lspDocument.CurrentSnapshot;
            return true;
        }
    }
}
