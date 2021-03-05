// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentSignatureHelpName)]
    internal class SignatureHelpHandler : IRequestHandler<TextDocumentPositionParams, SignatureHelp>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public SignatureHelpHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider)
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

            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;

            _logger = loggerProvider.CreateLogger(nameof(SignatureHelpHandler));
        }

        public async Task<SignatureHelp> HandleRequestAsync(TextDocumentPositionParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (clientCapabilities is null)
            {
                throw new ArgumentNullException(nameof(clientCapabilities));
            }

            _logger.LogInformation($"Starting request for {request.TextDocument.Uri}.");

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                _logger.LogWarning($"Failed to find document {request.TextDocument.Uri}.");
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(
                documentSnapshot,
                request.Position,
                cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var textDocumentPositionParams = new TextDocumentPositionParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                },
            };

            _logger.LogInformation($"Requesting signature help for {projectionResult.Uri}.");

            var signatureHelp = await _requestInvoker.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, SignatureHelp>(
                Methods.TextDocumentSignatureHelpName,
                projectionResult.LanguageKind.ToContainedLanguageContentType(),
                textDocumentPositionParams,
                cancellationToken).ConfigureAwait(false);

            if (signatureHelp is null)
            {
                _logger.LogInformation("Returning no result.");
            }
            else
            {
                _logger.LogInformation("Returning result.");
            }

            return signatureHelp;
        }
    }
}
