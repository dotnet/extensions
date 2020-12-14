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
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentReferencesName)]
    internal class FindAllReferencesHandler :
        LSPProgressListenerHandlerBase<ReferenceParams, VSReferenceItem[]>,
        IRequestHandler<ReferenceParams, VSReferenceItem[]>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly LSPProgressListener _lspProgressListener;

        [ImportingConstructor]
        public FindAllReferencesHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
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

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
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
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
            _lspProgressListener = lspProgressListener;
        }

        // Internal for testing
        internal async override Task<VSReferenceItem[]> HandleRequestAsync(ReferenceParams request, ClientCapabilities clientCapabilities, string token, CancellationToken cancellationToken)
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

            cancellationToken.ThrowIfCancellationRequested();

            var referenceParams = new SerializableReferenceParams()
            {
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                },
                Context = request.Context,
                PartialResultToken = token // request.PartialResultToken
            };

            if (!_lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: (value, ct) => ProcessReferenceItemsAsync(value, request.PartialResultToken, ct),
                WaitForProgressNotificationTimeout,
                cancellationToken,
                out var onCompleted))
            {
                return null;
            }

            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<SerializableReferenceParams, VSReferenceItem[]>(
                Methods.TextDocumentReferencesName,
                projectionResult.LanguageKind.ToContainedLanguageContentType(),
                referenceParams,
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // We must not return till we have received the progress notifications
            // and reported the results via the PartialResultToken
            await onCompleted.ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Results returned through Progress notification
            var remappedResults = await RemapReferenceItemsAsync(result, cancellationToken).ConfigureAwait(false);
            return remappedResults;
        }

        private async Task ProcessReferenceItemsAsync(
            JToken value,
            IProgress<object> progress,
            CancellationToken cancellationToken)
        {
            var result = value.ToObject<VSReferenceItem[]>();

            if (result == null || result.Length == 0)
            {
                return;
            }

            var remappedResults = await RemapReferenceItemsAsync(result, cancellationToken).ConfigureAwait(false);

            progress.Report(remappedResults);
        }

        private async Task<VSReferenceItem[]> RemapReferenceItemsAsync(VSReferenceItem[] result, CancellationToken cancellationToken)
        {
            var remappedLocations = new List<VSReferenceItem>();

            foreach (var referenceItem in result)
            {
                if (referenceItem?.Location is null || referenceItem.Text is null)
                {
                    continue;
                }

                // Temporary fix for codebehind leaking through
                // Revert when https://github.com/dotnet/aspnetcore/issues/22512 is resolved
                referenceItem.DefinitionText = FilterReferenceDisplayText(referenceItem.DefinitionText);
                referenceItem.Text = FilterReferenceDisplayText(referenceItem.Text);

                if (!RazorLSPConventions.IsVirtualCSharpFile(referenceItem.Location.Uri) &&
                    !RazorLSPConventions.IsVirtualHtmlFile(referenceItem.Location.Uri))
                {
                    // This location doesn't point to a virtual cs file. No need to remap.
                    remappedLocations.Add(referenceItem);
                    continue;
                }

                var razorDocumentUri = RazorLSPConventions.GetRazorDocumentUri(referenceItem.Location.Uri);
                var languageKind = RazorLSPConventions.IsVirtualCSharpFile(referenceItem.Location.Uri) ? RazorLanguageKind.CSharp : RazorLanguageKind.Html;
                var mappingResult = await _documentMappingProvider.MapToDocumentRangesAsync(
                    languageKind,
                    razorDocumentUri,
                    new[] { referenceItem.Location.Range },
                    cancellationToken).ConfigureAwait(false);

                if (mappingResult == null ||
                    mappingResult.Ranges[0].IsUndefined() ||
                    (_documentManager.TryGetDocument(razorDocumentUri, out var mappedDocumentSnapshot) &&
                    mappingResult.HostDocumentVersion != mappedDocumentSnapshot.Version))
                {
                    // Couldn't remap the location or the document changed in the meantime. Discard this location.
                    continue;
                }

                referenceItem.Location.Uri = razorDocumentUri;
                referenceItem.DisplayPath = razorDocumentUri.AbsolutePath;
                referenceItem.Location.Range = mappingResult.Ranges[0];

                remappedLocations.Add(referenceItem);
            }

            return remappedLocations.ToArray();
        }

        private object FilterReferenceDisplayText(object referenceText)
        {
            const string codeBehindObjectPrefix = "__o = ";
            const string codeBehindBackingFieldSuffix = "k__BackingField";

            if (referenceText is string text)
            {
                if (text.StartsWith(codeBehindObjectPrefix, StringComparison.Ordinal))
                {
                    return text
                        .Substring(codeBehindObjectPrefix.Length, text.Length - codeBehindObjectPrefix.Length - 1); // -1 for trailing `;`
                }

                return text.Replace(codeBehindBackingFieldSuffix, string.Empty);
            }

            if (referenceText is ClassifiedTextElement textElement &&
                FilterReferenceClassifiedRuns(textElement.Runs))
            {
                var filteredRuns = textElement.Runs.Skip(4); // `__o`, ` `, `=`, ` `
                filteredRuns = filteredRuns.Take(filteredRuns.Count() - 1); // Trailing `;`
                return new ClassifiedTextElement(filteredRuns);
            }

            return referenceText;
        }

        private bool FilterReferenceClassifiedRuns(IEnumerable<ClassifiedTextRun> runs)
        {
            if (runs.Count() < 5)
            {
                return false;
            }

            return VerifyRunMatches(runs.ElementAt(0), "field name", "__o") &&
                VerifyRunMatches(runs.ElementAt(1), "text", " ") &&
                VerifyRunMatches(runs.ElementAt(2), "operator", "=") &&
                VerifyRunMatches(runs.ElementAt(3), "text", " ") &&
                VerifyRunMatches(runs.Last(), "punctuation", ";");

            static bool VerifyRunMatches(ClassifiedTextRun run, string expectedClassificationType, string expectedText)
            {
                return run.ClassificationTypeName == expectedClassificationType &&
                    run.Text == expectedText;
            }
        }

        // Temporary while the PartialResultToken serialization fix is in
        [DataContract]
        private class SerializableReferenceParams : TextDocumentPositionParams
        {
            [DataMember(Name = "context")]
            public ReferenceContext Context { get; set; }

            [DataMember(Name = "partialResultToken")]
            public string PartialResultToken { get; set; }
        }
    }
}
