// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : IRequestHandler<TextDocumentPositionParams, Hover>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public HoverHandler(
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

        public async Task<Hover> HandleRequestAsync(TextDocumentPositionParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
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

            var contentType = projectionResult.LanguageKind.ToContainedLanguageContentType();

            cancellationToken.ThrowIfCancellationRequested();

            var textDocumentPositionParams = new TextDocumentPositionParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                }
            };

            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Hover>(
                Methods.TextDocumentHoverName,
                contentType,
                textDocumentPositionParams,
                cancellationToken).ConfigureAwait(false);

            if (result?.Range == null || result?.Contents == null)
            {
                return null;
            }

            var mappingResult = await _documentMappingProvider.MapToDocumentRangesAsync(projectionResult.LanguageKind, request.TextDocument.Uri, new[] { result.Range }, cancellationToken).ConfigureAwait(false);
            if (mappingResult == null || mappingResult.Ranges[0].IsUndefined())
            {
                // Couldn't remap the edits properly. Returning hover at initial request position.
                return CreateHover(result, new Range
                {
                    Start = request.Position,
                    End = request.Position
                });
            }
            else if (mappingResult.HostDocumentVersion != documentSnapshot.Version)
            {
                // Discard hover if document has changed.
                return null;
            }

            return CreateHover(result, mappingResult.Ranges[0]);
        }

        private static VSHover CreateHover(Hover originalHover, Range range)
        {
            return new VSHover
            {
                Contents = originalHover.Contents,
                Range = range,
                RawContent = originalHover is VSHover originalVSHover ? originalVSHover.RawContent : null
            };
        }
    }
}
