// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
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
        private readonly Dictionary<Uri, DocumentTracker> _documents;

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
            _documents = new Dictionary<Uri, DocumentTracker>();
        }

        public override void TrackDocument(ITextBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            var uri = _fileUriProvider.GetOrCreate(buffer);
            if (!_documents.TryGetValue(uri, out var documentTracker))
            {
                var lspDocument = _documentFactory.Create(buffer);
                documentTracker = new DocumentTracker(lspDocument);
                _documents[uri] = documentTracker;
            }

            documentTracker.Refcount++;
        }

        public override void UntrackDocument(ITextBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            var uri = _fileUriProvider.GetOrCreate(buffer);
            if (!_documents.TryGetValue(uri, out var documentTracker))
            {
                // We don't know about this document, noop.
                return;
            }

            documentTracker.Refcount--;

            if (documentTracker.Refcount == 0)
            {
                _documents.Remove(uri);
            }
        }

        public override bool TryGetDocument(Uri uri, out LSPDocument lspDocument)
        {
            Debug.Assert(_joinableTaskContext.IsOnMainThread);

            if (!_documents.TryGetValue(uri, out var documentTracker))
            {
                // This should never happen in practice but return `null` so our tests can validate
                lspDocument = null;
                return false;
            }

            lspDocument = documentTracker.Document;
            return true;
        }

        private class DocumentTracker
        {
            public DocumentTracker(LSPDocument document)
            {
                Document = document;
            }

            public LSPDocument Document { get; }

            public int Refcount { get; set; }
        }
    }
}
