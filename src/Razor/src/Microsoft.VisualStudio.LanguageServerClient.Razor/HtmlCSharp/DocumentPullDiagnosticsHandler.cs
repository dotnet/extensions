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
    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagnosticsHandler :
        IRequestHandler<DocumentDiagnosticsParams, DiagnosticReport[]>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPDocumentSynchronizer _documentSynchronizer;
        private readonly LSPDiagnosticsTranslator _diagnosticsProvider;

        [ImportingConstructor]
        public DocumentPullDiagnosticsHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPDocumentSynchronizer documentSynchronizer,
            LSPDiagnosticsTranslator diagnosticsProvider)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (documentSynchronizer is null)
            {
                throw new ArgumentNullException(nameof(documentSynchronizer));
            }

            if (diagnosticsProvider is null)
            {
                throw new ArgumentNullException(nameof(diagnosticsProvider));
            }

            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _documentSynchronizer = documentSynchronizer;
            _diagnosticsProvider = diagnosticsProvider;
        }

        // Internal for testing
        public async Task<DiagnosticReport[]> HandleRequestAsync(DocumentDiagnosticsParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
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

            if (!documentSnapshot.TryGetVirtualDocument<CSharpVirtualDocumentSnapshot>(out var csharpDoc))
            {
                return null;
            }

            var synchronized = await _documentSynchronizer.TrySynchronizeVirtualDocumentAsync(
                documentSnapshot.Version,
                csharpDoc,
                cancellationToken).ConfigureAwait(false);
            if (!synchronized)
            {
                // Could not synchronize, report nothing changed
                return new DiagnosticReport[]
                {
                    new DiagnosticReport()
                    {
                        ResultId = request.PreviousResultId,
                        Diagnostics = null
                    }
                };
            }

            var referenceParams = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = csharpDoc.Uri
                },
                PreviousResultId = request.PreviousResultId
            };

            // End goal is to transition this from ReinvokeRequestOnMultipleServersAsync -> ReinvokeRequestOnServerAsync
            // We can't do this right now as we don't have the ability to specify the language client name we'd like to make the call out to
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1246135
            var resultsFromAllLanguageServers = await _requestInvoker.ReinvokeRequestOnMultipleServersAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                MSLSPMethods.DocumentPullDiagnosticName,
                RazorLSPConstants.CSharpContentTypeName,
                referenceParams,
                cancellationToken).ConfigureAwait(false);

            var result = resultsFromAllLanguageServers.SelectMany(l => l).ToArray();

            var processedResults = await RemapDocumentDiagnosticsAsync(
                result,
                request.TextDocument.Uri,
                cancellationToken).ConfigureAwait(false);

            // | ---------------------------------------------------------------------------------- |
            // |                       LSP Platform Expected Response Semantics                     |
            // | ---------------------------------------------------------------------------------- |
            // | DiagnosticReport.Diagnostics     | DiagnosticReport.ResultId | Meaning             |
            // | -------------------------------- | ------------------------- | ------------------- |
            // | `null`                           | `null`                    | document gone       |
            // | `null`                           | valid                     | nothing changed     |
            // | valid (non-null including empty) | valid                     | diagnostics changed |
            // | ---------------------------------------------------------------------------------- |
            return processedResults;
        }

        private async Task<DiagnosticReport[]> RemapDocumentDiagnosticsAsync(
            DiagnosticReport[] unmappedDiagnosticReports,
            Uri razorDocumentUri,
            CancellationToken cancellationToken)
        {
            if (unmappedDiagnosticReports?.Any() != true)
            {
                return unmappedDiagnosticReports;
            }

            var mappedDiagnosticReports = new List<DiagnosticReport>(unmappedDiagnosticReports.Length);

            foreach (var diagnosticReport in unmappedDiagnosticReports)
            {
                // Check if there are any diagnostics in this report
                if (diagnosticReport?.Diagnostics?.Any() != true)
                {
                    mappedDiagnosticReports.Add(diagnosticReport);
                    continue;
                }

                var processedDiagnostics = await _diagnosticsProvider.TranslateAsync(
                    RazorLanguageKind.CSharp,
                    razorDocumentUri,
                    diagnosticReport.Diagnostics,
                    cancellationToken
                ).ConfigureAwait(false);

                if (!_documentManager.TryGetDocument(razorDocumentUri, out var documentSnapshot) ||
                    documentSnapshot.Version != processedDiagnostics.HostDocumentVersion)
                {
                    // We choose to discard diagnostics in this case & report nothing changed.
                    diagnosticReport.Diagnostics = null;
                    mappedDiagnosticReports.Add(diagnosticReport);
                    continue;
                }

                diagnosticReport.Diagnostics = processedDiagnostics.Diagnostics;

                mappedDiagnosticReports.Add(diagnosticReport);
            }

            return mappedDiagnosticReports.ToArray();
        }
    }
}
