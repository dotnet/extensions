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
    [ExportLspMethod(Methods.TextDocumentRenameName)]
    internal class RenameHandler : IRequestHandler<RenameParams, WorkspaceEdit>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public RenameHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider,
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

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;

            _logger = loggerProvider.CreateLogger(nameof(RenameHandler));
        }

        public async Task<WorkspaceEdit> HandleRequestAsync(RenameParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
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

            var renameParams = new RenameParams()
            {
                Position = projectionResult.Position,
                NewName = request.NewName,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                }
            };

            _logger.LogInformation($"Requesting rename for {projectionResult.Uri}.");

            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<RenameParams, WorkspaceEdit>(
                Methods.TextDocumentRenameName,
                projectionResult.LanguageKind.ToContainedLanguageContentType(),
                renameParams,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                _logger.LogInformation("Received no result.");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Received result, remapping.");

            var remappedResult = await _documentMappingProvider.RemapWorkspaceEditAsync(result, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Returned rename result.");
            return remappedResult;
        }
    }
}
