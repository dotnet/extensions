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
    [ExportLspMethod(MSLSPMethods.OnAutoInsertName)]
    internal class OnAutoInsertHandler : IRequestHandler<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>
    {
        private static readonly IReadOnlyList<string> AllowedTriggerCharacters = new[] { ">", "=", "-" };

        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public OnAutoInsertHandler(
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

        public async Task<DocumentOnAutoInsertResponseItem> HandleRequestAsync(DocumentOnAutoInsertParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (!AllowedTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                // We haven't built support for this character yet.
                return null;
            }

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null || projectionResult.LanguageKind != RazorLanguageKind.Html)
            {
                return null;
            }

            var formattingParams = new DocumentOnAutoInsertParams()
            {
                Character = request.Character,
                Options = request.Options,
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier() { Uri = projectionResult.Uri }
            };

            var contentType = projectionResult.LanguageKind.ToContainedLanguageContentType();
            var response = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(
                MSLSPMethods.OnAutoInsertName,
                contentType,
                formattingParams,
                cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return null;
            }

            var remappedEdit = await _documentMappingProvider.RemapTextEditsAsync(projectionResult.Uri, new[] { response.TextEdit }, cancellationToken).ConfigureAwait(false);

            if (!remappedEdit.Any())
            {
                return null;
            }
            var remappedResponse = new DocumentOnAutoInsertResponseItem()
            {
                TextEdit = remappedEdit.Single(),
                TextEditFormat = response.TextEditFormat,
            };

            return remappedResponse;
        }
    }
}
