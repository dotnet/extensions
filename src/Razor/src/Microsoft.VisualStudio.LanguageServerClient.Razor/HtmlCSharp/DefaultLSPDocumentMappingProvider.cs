// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPDocumentMappingProvider))]
    internal class DefaultLSPDocumentMappingProvider : LSPDocumentMappingProvider
    {
        private static readonly TextEdit[] EmptyEdits = Array.Empty<TextEdit>();

        private readonly LSPRequestInvoker _requestInvoker;

        // Lazy loading the document manager to get around circular dependencies
        // The Document Manager is a more "Core Service" it depends on the ChangeTriggers which require the LSPDocumentMappingProvider
        // LSPDocumentManager => LSPDocumentMappingProvider => LSPDocumentManagerChangeTrigger => LSPDocumentManager
        private readonly Lazy<LSPDocumentManager> _lazyDocumentManager;

        [ImportingConstructor]
        public DefaultLSPDocumentMappingProvider(LSPRequestInvoker requestInvoker, Lazy<LSPDocumentManager> lazyDocumentManager)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (lazyDocumentManager is null)
            {
                throw new ArgumentNullException(nameof(lazyDocumentManager));
            }

            _requestInvoker = requestInvoker;
            _lazyDocumentManager = lazyDocumentManager;
        }

        public override Task<RazorMapToDocumentRangesResponse> MapToDocumentRangesAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range[] projectedRanges, CancellationToken cancellationToken)
            => MapToDocumentRangesAsync(languageKind, razorDocumentUri, projectedRanges, LanguageServerMappingBehavior.Strict, cancellationToken);

        public async override Task<RazorMapToDocumentRangesResponse> MapToDocumentRangesAsync(
            RazorLanguageKind languageKind, 
            Uri razorDocumentUri, 
            Range[] projectedRanges, 
            LanguageServerMappingBehavior mappingBehavior, 
            CancellationToken cancellationToken)
        {
            if (razorDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(razorDocumentUri));
            }

            if (projectedRanges is null)
            {
                throw new ArgumentNullException(nameof(projectedRanges));
            }

            var mapToDocumentRangeParams = new RazorMapToDocumentRangesParams()
            {
                Kind = languageKind,
                RazorDocumentUri = razorDocumentUri,
                ProjectedRanges = projectedRanges,
                MappingBehavior = mappingBehavior,
            };

            var documentMappingResponse = await _requestInvoker.ReinvokeRequestOnServerAsync<RazorMapToDocumentRangesParams, RazorMapToDocumentRangesResponse>(
                LanguageServerConstants.RazorMapToDocumentRangesEndpoint,
                RazorLSPConstants.RazorLSPContentTypeName,
                mapToDocumentRangeParams,
                cancellationToken).ConfigureAwait(false);

            return documentMappingResponse;
        }

        public async override Task<Location[]> RemapLocationsAsync(Location[] locations, CancellationToken cancellationToken)
        {
            if (locations is null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            var remappedLocations = new List<Location>();
            foreach (var location in locations)
            {
                var uri = location.Uri;
                RazorLanguageKind languageKind;
                if (RazorLSPConventions.IsRazorCSharpFile(uri))
                {
                    languageKind = RazorLanguageKind.CSharp;
                }
                else if (RazorLSPConventions.IsRazorHtmlFile(uri))
                {
                    languageKind = RazorLanguageKind.Html;
                }
                else
                {
                    // This location doesn't point to a virtual razor file. No need to remap.
                    remappedLocations.Add(location);
                    continue;
                }

                var razorDocumentUri = RazorLSPConventions.GetRazorDocumentUri(uri);

                var mappingResult = await MapToDocumentRangesAsync(
                    languageKind,
                    razorDocumentUri,
                    new[] { location.Range },
                    cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (mappingResult == null ||
                    (_lazyDocumentManager.Value.TryGetDocument(razorDocumentUri, out var documentSnapshot) &&
                    mappingResult.HostDocumentVersion != documentSnapshot.Version))
                {
                    // Couldn't remap the location or the document changed in the meantime. Discard these ranges.
                    continue;
                }

                var remappedRange = mappingResult.Ranges[0];
                if (remappedRange.IsUndefined())
                {
                    // Couldn't remap the range correctly. Discard this range.
                    continue;
                }

                var remappedLocation = new Location()
                {
                    Uri = razorDocumentUri,
                    Range = remappedRange,
                };

                remappedLocations.Add(remappedLocation);
            }

            return remappedLocations.ToArray();
        }

        public async override Task<TextEdit[]> RemapTextEditsAsync(Uri uri, TextEdit[] edits, CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (edits is null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            if (!RazorLSPConventions.IsRazorCSharpFile(uri) && !RazorLSPConventions.IsRazorHtmlFile(uri))
            {
                // This is not a virtual razor file. No need to remap.
                return edits;
            }

            var (_, remappedEdits) = await RemapTextEditsCoreAsync(uri, edits, TextEditKind.Default, cancellationToken).ConfigureAwait(false);
            return remappedEdits;
        }

        public async override Task<TextEdit[]> RemapFormattedTextEditsAsync(Uri uri, TextEdit[] edits, FormattingOptions options, bool containsSnippet, CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (edits is null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            if (!RazorLSPConventions.IsRazorCSharpFile(uri) && !RazorLSPConventions.IsRazorHtmlFile(uri))
            {
                // This is not a virtual razor file. No need to remap.
                return edits;
            }

            var textEditKind = containsSnippet ? TextEditKind.Snippet : TextEditKind.FormatOnType;
            var (_, remappedEdits) = await RemapTextEditsCoreAsync(uri, edits, textEditKind, cancellationToken, formattingOptions: options).ConfigureAwait(false);
            return remappedEdits;
        }

        public async override Task<WorkspaceEdit> RemapWorkspaceEditAsync(WorkspaceEdit workspaceEdit, CancellationToken cancellationToken)
        {
            if (TryGetDocumentChanges(workspaceEdit, out var documentChanges))
            {
                // The LSP spec says, we should prefer `DocumentChanges` property over `Changes` if available.
                var remappedEdits = await RemapVersionedDocumentEditsAsync(documentChanges, cancellationToken).ConfigureAwait(false);
                return new WorkspaceEdit()
                {
                    DocumentChanges = remappedEdits
                };
            }
            else if (workspaceEdit?.Changes != null)
            {
                var remappedEdits = await RemapDocumentEditsAsync(workspaceEdit.Changes, cancellationToken).ConfigureAwait(false);
                return new WorkspaceEdit()
                {
                    Changes = remappedEdits
                };
            }

            return workspaceEdit;
        }

        private bool TryGetDocumentChanges(WorkspaceEdit workspaceEdit, out TextDocumentEdit[] documentChanges)
        {
            documentChanges = null;

            if (workspaceEdit.DocumentChanges?.Value is TextDocumentEdit[] documentEdits)
            {
                documentChanges = documentEdits;
                return true;
            }

            if (workspaceEdit.DocumentChanges?.Value is SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] sumTypeArray)
            {
                var documentEditList = new List<TextDocumentEdit>();
                foreach (var sumType in sumTypeArray)
                {
                    if (sumType.Value is TextDocumentEdit textDocumentEdit)
                    {
                        documentEditList.Add(textDocumentEdit);
                    }
                }

                if (documentEditList.Count > 0)
                {
                    documentChanges = documentEditList.ToArray();
                    return true;
                }
            }

            return false;
        }

        private async Task<TextDocumentEdit[]> RemapVersionedDocumentEditsAsync(TextDocumentEdit[] documentEdits, CancellationToken cancellationToken)
        {
            var remappedDocumentEdits = new List<TextDocumentEdit>();
            foreach (var entry in documentEdits)
            {
                var uri = entry.TextDocument.Uri;
                if (!CanRemap(uri))
                {
                    // This location doesn't point to a background razor file. No need to remap.
                    remappedDocumentEdits.Add(entry);

                    continue;
                }

                var edits = entry.Edits;
                var (documentSnapshot, remappedEdits) = await RemapTextEditsCoreAsync(uri, edits, TextEditKind.Default, cancellationToken).ConfigureAwait(false);
                if (remappedEdits == null || remappedEdits.Length == 0)
                {
                    // Nothing to do.
                    continue;
                }

                var razorDocumentUri = RazorLSPConventions.GetRazorDocumentUri(uri);
                remappedDocumentEdits.Add(new TextDocumentEdit()
                {
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Uri = razorDocumentUri,
                        Version = documentSnapshot?.Version
                    },
                    Edits = remappedEdits
                });
            }

            return remappedDocumentEdits.ToArray();
        }

        private async Task<Dictionary<string, TextEdit[]>> RemapDocumentEditsAsync(Dictionary<string, TextEdit[]> changes, CancellationToken cancellationToken)
        {
            var remappedChanges = new Dictionary<string, TextEdit[]>();
            foreach (var entry in changes)
            {
                var uri = new Uri(entry.Key);
                var edits = entry.Value;

                if (!CanRemap(uri))
                {
                    // This location doesn't point to a background razor file. No need to remap.
                    remappedChanges[entry.Key] = entry.Value;
                    continue;
                }

                var (_, remappedEdits) = await RemapTextEditsCoreAsync(uri, edits, TextEditKind.Default, cancellationToken).ConfigureAwait(false);
                if (remappedEdits == null || remappedEdits.Length == 0)
                {
                    // Nothing to do.
                    continue;
                }

                var razorDocumentUri = RazorLSPConventions.GetRazorDocumentUri(uri);
                remappedChanges[razorDocumentUri.AbsoluteUri] = remappedEdits;
            }

            return remappedChanges;
        }

        private async Task<(LSPDocumentSnapshot, TextEdit[])> RemapTextEditsCoreAsync(
            Uri uri,
            TextEdit[] edits,
            TextEditKind textEditKind,
            CancellationToken cancellationToken,
            FormattingOptions formattingOptions = null)
        {
            var languageKind = RazorLanguageKind.Razor;
            if (RazorLSPConventions.IsRazorCSharpFile(uri))
            {
                languageKind = RazorLanguageKind.CSharp;
            }
            else if (RazorLSPConventions.IsRazorHtmlFile(uri))
            {
                languageKind = RazorLanguageKind.Html;
            }
            else
            {
                Debug.Fail("This method should only be called for Razor background files.");
            }

            var razorDocumentUri = RazorLSPConventions.GetRazorDocumentUri(uri);

            var mapToDocumentEditsParams = new RazorMapToDocumentEditsParams()
            {
                Kind = languageKind,
                RazorDocumentUri = razorDocumentUri,
                ProjectedTextEdits = edits,
                TextEditKind = textEditKind,
                FormattingOptions = formattingOptions
            };

            var mappingResult = await _requestInvoker.ReinvokeRequestOnServerAsync<RazorMapToDocumentEditsParams, RazorMapToDocumentEditsResponse>(
                LanguageServerConstants.RazorMapToDocumentEditsEndpoint,
                RazorLSPConstants.RazorLSPContentTypeName,
                mapToDocumentEditsParams,
                cancellationToken).ConfigureAwait(false);

            if (mappingResult == null ||
                (_lazyDocumentManager.Value.TryGetDocument(razorDocumentUri, out var documentSnapshot) &&
                    mappingResult.HostDocumentVersion != documentSnapshot.Version))
            {
                // Couldn't remap the location or the document changed in the meantime. Discard these ranges.
                return (null, EmptyEdits);
            }

            return (documentSnapshot, mappingResult.TextEdits);
        }

        private static bool CanRemap(Uri uri)
        {
            return RazorLSPConventions.IsRazorCSharpFile(uri) || RazorLSPConventions.IsRazorHtmlFile(uri);
        }
    }
}
