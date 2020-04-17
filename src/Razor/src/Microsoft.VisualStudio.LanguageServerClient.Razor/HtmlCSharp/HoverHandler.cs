// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentHoverName)]
    internal class HoverHandler : IRequestHandler<TextDocumentPositionParams, Hover>
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public HoverHandler(
            JoinableTaskContext joinableTaskContext,
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

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

            _joinableTaskFactory = joinableTaskContext.Factory;
            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
        }

        public async Task<Hover> HandleRequestAsync(TextDocumentPositionParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            // Switch to a background thread.
            await TaskScheduler.Default;

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                return null;
            }

            var serverKind = projectionResult.LanguageKind == RazorLanguageKind.CSharp ? LanguageServerKind.CSharp : LanguageServerKind.Html;

            if (serverKind != LanguageServerKind.CSharp)
            {
                // Hover is only supported in the C# LSP for now.
                return null;
            }

            var textDocumentPositionParams = new TextDocumentPositionParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                }
            };

            Hover result;
            try
            {
                result = await _requestInvoker.RequestServerAsync<TextDocumentPositionParams, Hover>(
                    Methods.TextDocumentHoverName,
                    serverKind,
                    textDocumentPositionParams,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ensure we fail silently (Temporary till roslyn update is live)
                return null;
            }

            if (result?.Range == null || result?.Contents == null)
            {
                return null;
            }

            var mappingResult = await _documentMappingProvider.MapToDocumentRangeAsync(projectionResult.LanguageKind, request.TextDocument.Uri, result.Range, cancellationToken).ConfigureAwait(false);

            if (mappingResult == null)
            {
                // Couldn't remap the edits properly. Returning hover at initial request position.
                return new Hover
                {
                    Contents = result.Contents,
                    Range = new Range()
                    {
                        Start = request.Position,
                        End = request.Position
                    }
                };
            }
            else if (mappingResult.HostDocumentVersion != documentSnapshot.Version)
            {
                // Discard hover if document has changed.
                return null;
            }

            return new Hover
            {
                Contents = result.Contents,
                Range = mappingResult.Range
            };
        }
    }
}
