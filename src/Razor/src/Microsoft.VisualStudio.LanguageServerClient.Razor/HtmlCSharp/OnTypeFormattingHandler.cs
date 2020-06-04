// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentOnTypeFormattingName)]
    internal class OnTypeFormattingHandler : IRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        private static readonly TextEdit[] EmptyEdits = Array.Empty<TextEdit>();
        private static readonly IReadOnlyList<string> AllowedTriggerCharacters = new[] { ">", "=", "-" };

        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly LSPEditorService _editorService;

        [ImportingConstructor]
        public OnTypeFormattingHandler(
            LSPDocumentManager documentManager,
            LSPRequestInvoker requestInvoker,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider,
            LSPEditorService editorService)
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

            if (editorService is null)
            {
                throw new ArgumentNullException(nameof(editorService));
            }

            _documentManager = documentManager;
            _requestInvoker = requestInvoker;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
            _editorService = editorService;
        }

        public async Task<TextEdit[]> HandleRequestAsync(DocumentOnTypeFormattingParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (!AllowedTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                // We haven't built support for this character yet.
                return EmptyEdits;
            }

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return EmptyEdits;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null || projectionResult.LanguageKind != RazorLanguageKind.Html)
            {
                return EmptyEdits;
            }

            if (request.Options.OtherOptions == null)
            {
                request.Options.OtherOptions = new Dictionary<string, object>();
            }
            request.Options.OtherOptions[LanguageServerConstants.ExpectsCursorPlaceholderKey] = true;

            var formattingParams = new DocumentOnTypeFormattingParams()
            {
                Character = request.Character,
                Options = request.Options,
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier() { Uri = projectionResult.Uri }
            };

            var serverKind = projectionResult.LanguageKind == RazorLanguageKind.CSharp ? LanguageServerKind.CSharp : LanguageServerKind.Html;
            var edits = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(
                Methods.TextDocumentOnTypeFormattingName,
                serverKind,
                formattingParams,
                cancellationToken).ConfigureAwait(false);

            if (edits == null)
            {
                return EmptyEdits;
            }

            var mappedEdits = await _documentMappingProvider.RemapTextEditsAsync(projectionResult.Uri, edits, cancellationToken).ConfigureAwait(false);
            
            await _editorService.ApplyTextEditsAsync(documentSnapshot.Uri, documentSnapshot.Snapshot, mappedEdits).ConfigureAwait(false);

            // We would have already applied the edits and moved the cursor. Return empty.
            return EmptyEdits;
        }
    }
}
