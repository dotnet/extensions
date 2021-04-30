// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(MSLSPMethods.OnAutoInsertName)]
    internal class OnAutoInsertHandler : IRequestHandler<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>
    {
        private static readonly HashSet<string> HTMLAllowedTriggerCharacters = new() { ">", "=", "-" };
        private static readonly HashSet<string> CSharpAllowedTriggerCharacters = new() { "'", "/", "\n" };
        private static readonly HashSet<string> AllAllowedTriggerCharacters = HTMLAllowedTriggerCharacters
            .Concat(CSharpAllowedTriggerCharacters)
            .ToHashSet();

        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public OnAutoInsertHandler(
            LSPDocumentManager documentManager,
            LSPRequestInvoker requestInvoker,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider)
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

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            _documentManager = documentManager;
            _requestInvoker = requestInvoker;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;

            _logger = loggerProvider.CreateLogger(nameof(OnAutoInsertHandler));
        }

        public async Task<DocumentOnAutoInsertResponseItem> HandleRequestAsync(DocumentOnAutoInsertParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!AllAllowedTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                // We haven't built support for this character yet.
                return null;
            }

            _logger.LogInformation($"Starting request for {request.TextDocument.Uri}, with trigger character {request.Character}.");

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                _logger.LogWarning($"Failed to find document {request.TextDocument.Uri}.");
                return null;
            }
            else if (projectionResult.LanguageKind == RazorLanguageKind.Razor)
            {
                _logger.LogInformation("OnAutoInsert not supported in Razor context.");
                return null;
            }
            else if (projectionResult.LanguageKind == RazorLanguageKind.Html &&
                !HTMLAllowedTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                _logger.LogInformation("Inapplicable HTML trigger char.");
                return null;
            }
            else if (projectionResult.LanguageKind == RazorLanguageKind.CSharp &&
                !CSharpAllowedTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                _logger.LogInformation("Inapplicable C# trigger char.");
                return null;
            }

            var formattingParams = new DocumentOnAutoInsertParams()
            {
                Character = request.Character,
                Options = request.Options,
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier() { Uri = projectionResult.Uri }
            };

            _logger.LogInformation($"Requesting auto-insert for {projectionResult.Uri}.");

            var contentType = projectionResult.LanguageKind.ToContainedLanguageContentType();
            var response = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(
                MSLSPMethods.OnAutoInsertName,
                contentType,
                formattingParams,
                cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                _logger.LogInformation("Received no results.");
                return null;
            }

            _logger.LogInformation("Received result, remapping.");

            var containsSnippet = response.TextEditFormat == InsertTextFormat.Snippet;
            var remappedEdits = await _documentMappingProvider.RemapFormattedTextEditsAsync(
                projectionResult.Uri,
                new[] { response.TextEdit },
                request.Options,
                containsSnippet,
                cancellationToken).ConfigureAwait(false);

            if (!remappedEdits.Any())
            {
                _logger.LogInformation("No edits remain after remapping.");
                return null;
            }

            var remappedEdit = remappedEdits.Single();
            var remappedResponse = new DocumentOnAutoInsertResponseItem()
            {
                TextEdit = remappedEdit,
                TextEditFormat = response.TextEditFormat,
            };

            _logger.LogInformation($"Returning edit.");
            return remappedResponse;
        }
    }
}
