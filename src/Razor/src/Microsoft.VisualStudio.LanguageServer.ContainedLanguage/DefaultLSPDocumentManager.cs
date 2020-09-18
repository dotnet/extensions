// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    [Shared]
    [Export(typeof(LSPDocumentManager))]
    internal class DefaultLSPDocumentManager : TrackingLSPDocumentManager
    {
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentFactory _documentFactory;
        private readonly ConcurrentDictionary<Uri, LSPDocument> _documents;

        public override event EventHandler<LSPDocumentChangeEventArgs> Changed;

        [ImportingConstructor]
        public DefaultLSPDocumentManager(
            JoinableTaskContext joinableTaskContext,
            FileUriProvider fileUriProvider,
            LSPDocumentFactory documentFactory,
            [ImportMany] IEnumerable<LSPDocumentManagerChangeTrigger> changeTriggers)
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
            _documents = new ConcurrentDictionary<Uri, LSPDocument>();

            foreach (var trigger in changeTriggers)
            {
                trigger.Initialize(this);
            }
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

            // Given we're no longer tracking the document we don't want to pin the Uri to the current state of the buffer (could have been renamed to another Uri).
            _fileUriProvider.Remove(buffer);

            if (_documents.TryRemove(uri, out _))
            {
                var args = new LSPDocumentChangeEventArgs(lspDocument.CurrentSnapshot, @new: null, LSPDocumentChangeKind.Removed);
                Changed?.Invoke(this, args);
            }
            else
            {
                Debug.Fail($"Couldn't remove {uri.AbsolutePath}. This should never ever happen.");
            }

            lspDocument.Dispose();
        }

        [Obsolete("Use the int override instead")]
        public override void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<ITextChange> changes,
            long hostDocumentVersion)
        {
            UpdateVirtualDocument<TVirtualDocument>(hostDocumentUri, changes, (int)hostDocumentVersion);
        }

        public override void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<ITextChange> changes,
            int hostDocumentVersion)
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

            var virtualDocumentAcquired = lspDocument.TryGetVirtualDocument<TVirtualDocument>(out var virtualDocument);
            if (!virtualDocumentAcquired)
            {
                // Unable to locate virtual document of typeof(TVirtualDocument)
                // Ex. Microsoft.WebTools.Languages.LanguageServer.Delegation.ContainedLanguage.Css.CssVirtualDocument
                return;
            }

            if (changes.Count == 0 &&
                virtualDocument.HostDocumentVersion == hostDocumentVersion)
            {
                // The current virtual document already knows about this update.
                // Ignore it so we don't prematurely invoke a change event.
                return;
            }

            var old = lspDocument.CurrentSnapshot;
            var oldVirtual = virtualDocument.CurrentSnapshot;
            var @new = lspDocument.UpdateVirtualDocument<TVirtualDocument>(changes, hostDocumentVersion);

            if (old == @new)
            {
                return;
            }

            if (!lspDocument.TryGetVirtualDocument<TVirtualDocument>(out var newVirtualDocument))
            {
                throw new InvalidOperationException("This should never ever happen.");
            }

            var newVirtual = newVirtualDocument.CurrentSnapshot;
            var args = new LSPDocumentChangeEventArgs(
                old,
                @new,
                oldVirtual,
                newVirtual,
                LSPDocumentChangeKind.VirtualDocumentChanged);
            Changed?.Invoke(this, args);
        }

        public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
        {
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
