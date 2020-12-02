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
    [ExportLspMethod(MSLSPMethods.OnTypeRenameName)]
    internal class OnTypeRenameHandler : IRequestHandler<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>
    {
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        
        [ImportingConstructor]
        public OnTypeRenameHandler(
            LSPDocumentManager documentManager,
            LSPRequestInvoker requestInvoker,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            _documentManager = documentManager;
            _requestInvoker = requestInvoker;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
        }

        public async Task<DocumentOnTypeRenameResponseItem> HandleRequestAsync(DocumentOnTypeRenameParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult is null || projectionResult.LanguageKind != RazorLanguageKind.Html)
            {
                return null;
            }

            var onTypeRenameParams = new DocumentOnTypeRenameParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier() { Uri = projectionResult.Uri }
            };

            var contentType = projectionResult.LanguageKind.ToContainedLanguageContentType();
            var response = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>(
                MSLSPMethods.OnTypeRenameName,
                contentType,
                onTypeRenameParams,
                cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                return null;
            }

            var mappingResult = await _documentMappingProvider.MapToDocumentRangesAsync(
                projectionResult.LanguageKind,
                request.TextDocument.Uri,
                response.Ranges,
                cancellationToken).ConfigureAwait(false);

            if (mappingResult is null ||
                (_documentManager.TryGetDocument(request.TextDocument.Uri, out var mappedDocumentSnapshot) &&
                mappingResult.HostDocumentVersion != mappedDocumentSnapshot.Version))
            {
                // Couldn't remap the range or the document changed in the meantime. Discard this result.
                return null;
            }

            response.Ranges = mappingResult.Ranges;
            return response;
        }
    }
}
