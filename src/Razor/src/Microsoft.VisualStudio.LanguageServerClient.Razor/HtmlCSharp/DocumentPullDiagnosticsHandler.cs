// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagnosticsHandler :
        LSPProgressListenerHandlerBase<DocumentDiagnosticsParams, DiagnosticReport[]>,
        IRequestHandler<DocumentDiagnosticsParams, DiagnosticReport[]>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly LSPProgressListener _lspProgressListener;

        [ImportingConstructor]
        public DocumentPullDiagnosticsHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPDocumentMappingProvider documentMappingProvider,
            LSPProgressListener lspProgressListener)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            if (lspProgressListener is null)
            {
                throw new ArgumentNullException(nameof(lspProgressListener));
            }

            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _documentMappingProvider = documentMappingProvider;
            _lspProgressListener = lspProgressListener;
        }

        // Internal for testing
        internal async override Task<DiagnosticReport[]> HandleRequestAsync(DocumentDiagnosticsParams request, ClientCapabilities clientCapabilities, string token, CancellationToken cancellationToken)
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

            var referenceParams = new SerializableDocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = csharpDoc.Uri
                },
                PreviousResultId = request.PreviousResultId,
                PartialResultToken = token
            };

            if (!_lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: (value, ct) => ProcessDocumentDiagnosticsAsync(value, request.PartialResultToken, request.TextDocument.Uri, documentSnapshot, ct),
                WaitForProgressNotificationTimeout,
                cancellationToken,
                out var onCompleted))
            {
                return null;
            }

            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<SerializableDocumentDiagnosticsParams, DiagnosticReport[]>(
                MSLSPMethods.DocumentPullDiagnosticName,
                RazorLSPConstants.CSharpContentTypeName,
                referenceParams,
                cancellationToken).ConfigureAwait(false);

            // We must not return till we have received the progress notifications
            // and reported the results via the PartialResultToken
            await onCompleted.ConfigureAwait(false);

            return null;
        }

        private async Task ProcessDocumentDiagnosticsAsync(
            JToken value,
            IProgress<DiagnosticReport[]> progress,
            Uri razorDocumentUri,
            LSPDocumentSnapshot documentSnapshot,
            CancellationToken cancellationToken)
        {
            var result = value.ToObject<DiagnosticReport[]>();

            if (result == null || result.Length == 0)
            {
                return;
            }

            var remappedResults = await RemapDocumentDiagnosticsAsync(
                result,
                razorDocumentUri,
                documentSnapshot,
                cancellationToken).ConfigureAwait(false);

            progress.Report(remappedResults);
        }

        private async Task<DiagnosticReport[]> RemapDocumentDiagnosticsAsync(
            DiagnosticReport[] unmappedDiagnosticReports,
            Uri razorDocumentUri,
            LSPDocumentSnapshot documentSnapshot,
            CancellationToken cancellationToken)
        {
            if (unmappedDiagnosticReports?.Length == 0)
            {
                return unmappedDiagnosticReports;
            }

            var mappedDiagnosticReports = new List<DiagnosticReport>(unmappedDiagnosticReports.Length);

            foreach (var diagnosticReport in unmappedDiagnosticReports)
            {
                if (diagnosticReport?.Diagnostics?.Length == 0)
                {
                    mappedDiagnosticReports.Add(diagnosticReport);
                    continue;
                }

                var unmappedDiagnostics = diagnosticReport.Diagnostics;
                var mappedDiagnostics = new List<Diagnostic>(unmappedDiagnostics.Length);

                var rangesToMap = unmappedDiagnostics.Select(r => r.Range).ToArray();
                var mappingResult = await _documentMappingProvider.MapToDocumentRangesAsync(
                    RazorLanguageKind.CSharp,
                    razorDocumentUri,
                    rangesToMap,
                    LanguageServerMappingBehavior.Inclusive,
                    cancellationToken).ConfigureAwait(false);

                if (mappingResult == null || mappingResult.HostDocumentVersion != documentSnapshot.Version)
                {
                    // Couldn't remap the range or the document changed in the meantime. Discard this highlight.
                    return Array.Empty<DiagnosticReport>();
                }

                for (var i = 0; i < unmappedDiagnostics.Length; i++)
                {
                    var diagnostic = unmappedDiagnostics[i];
                    var range = mappingResult.Ranges[i];
                    if (range.IsUndefined())
                    {
                        // Couldn't remap the range correctly. Discard this range.
                        continue;
                    }

                    diagnostic.Range = range;
                    mappedDiagnostics.Add(diagnostic);
                }

                diagnosticReport.Diagnostics = mappedDiagnostics.ToArray();
                mappedDiagnosticReports.Add(diagnosticReport);
            }

            return mappedDiagnosticReports.ToArray();
        }

        [DataContract]
        private class SerializableDocumentDiagnosticsParams : DiagnosticParams
        {
            [DataMember(Name = "partialResultToken")]
            public string PartialResultToken { get; set; }
        }
    }
}
