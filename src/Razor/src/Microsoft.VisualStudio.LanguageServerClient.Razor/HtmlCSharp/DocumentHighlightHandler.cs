// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentDocumentHighlightName)]
    internal class DocumentHighlightHandler : IRequestHandler<DocumentHighlightParams, DocumentHighlight[]>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public DocumentHighlightHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
        }

        public async Task<DocumentHighlight[]> HandleRequestAsync(DocumentHighlightParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (clientCapabilities is null)
            {
                throw new ArgumentNullException(nameof(clientCapabilities));
            }

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                return null;
            }

            var serverKind = projectionResult.LanguageKind == RazorLanguageKind.CSharp ? LanguageServerKind.CSharp : LanguageServerKind.Html;

            cancellationToken.ThrowIfCancellationRequested();

            var documentHighlightParams = new DocumentHighlightParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                }
            };

            var highlights = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentHighlightParams, DocumentHighlight[]>(
                Methods.TextDocumentDocumentHighlightName,
                serverKind.ToContentType(),
                documentHighlightParams,
                cancellationToken).ConfigureAwait(false);

            if (highlights == null || highlights.Length == 0)
            {
                return highlights;
            }

            var remappedHighlights = new List<DocumentHighlight>();

            var rangesToMap = highlights.Select(r => r.Range).ToArray();
            var mappingResult = await _documentMappingProvider.MapToDocumentRangesAsync(
                projectionResult.LanguageKind,
                request.TextDocument.Uri,
                rangesToMap,
                cancellationToken).ConfigureAwait(false);

            if (mappingResult == null || mappingResult.HostDocumentVersion != documentSnapshot.Version)
            {
                // Couldn't remap the range or the document changed in the meantime. Discard this highlight.
                return Array.Empty<DocumentHighlight>();
            }

            for (var i = 0; i < highlights.Length; i++)
            {
                var highlight = highlights[i];
                var range = mappingResult.Ranges[i];
                if (range.IsUndefined())
                {
                    // Couldn't remap the range correctly. Discard this range.
                    continue;
                }

                var remappedHighlight = new DocumentHighlight()
                {
                    Range = range,
                    Kind = highlight.Kind
                };

                remappedHighlights.Add(remappedHighlight);
            }

            return remappedHighlights.ToArray();
        }
    }
}
