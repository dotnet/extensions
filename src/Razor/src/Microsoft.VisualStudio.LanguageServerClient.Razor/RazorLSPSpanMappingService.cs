// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal sealed class RazorLSPSpanMappingService : IRazorSpanMappingService
    {
        private readonly LSPDocumentMappingProvider _lspDocumentMappingProvider;

        private readonly ITextSnapshot _textSnapshot;
        private readonly LSPDocumentSnapshot _documentSnapshot;

        public RazorLSPSpanMappingService(
            LSPDocumentMappingProvider lspDocumentMappingProvider,
            LSPDocumentSnapshot documentSnapshot,
            ITextSnapshot textSnapshot)
        {
            if (lspDocumentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(lspDocumentMappingProvider));
            }

            if (textSnapshot == null)
            {
                throw new ArgumentNullException(nameof(textSnapshot));
            }

            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _lspDocumentMappingProvider = lspDocumentMappingProvider;

            _textSnapshot = textSnapshot;
            _documentSnapshot = documentSnapshot;
        }

        public async Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
            Document document,
            IEnumerable<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            return await MapSpansAsync(spans, _textSnapshot.AsText(), _documentSnapshot.Snapshot.AsText(), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
            IEnumerable<TextSpan> spans,
            SourceText sourceTextGenerated,
            SourceText sourceTextRazor,
            CancellationToken cancellationToken)
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            var projectedRanges = spans.Select(span => span.AsLSPRange(sourceTextGenerated)).ToArray();

            var mappedResult = await _lspDocumentMappingProvider.MapToDocumentRangesAsync(
                RazorLanguageKind.CSharp,
                _documentSnapshot.Uri,
                projectedRanges,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var mappedSpanResults = GetMappedSpanResults(_documentSnapshot, sourceTextRazor, mappedResult);
            return mappedSpanResults;
        }

        // Internal for testing
        internal static ImmutableArray<RazorMappedSpanResult> GetMappedSpanResults(
            LSPDocumentSnapshot documentSnapshot,
            SourceText sourceTextRazor,
            RazorMapToDocumentRangesResponse mappedResult)
        {
            var results = ImmutableArray.CreateBuilder<RazorMappedSpanResult>();

            if (mappedResult is null)
            {
                return results.ToImmutable();
            }

            foreach (var mappedRange in mappedResult.Ranges)
            {
                if (RangeExtensions.IsUndefined(mappedRange))
                {
                    // Couldn't remap the range correctly. Add default placeholder to indicate to C# that there were issues.
                    results.Add(new RazorMappedSpanResult());
                    continue;
                }

                var mappedSpan = mappedRange.AsTextSpan(sourceTextRazor);
                var linePositionSpan = sourceTextRazor.Lines.GetLinePositionSpan(mappedSpan);
                var filePath = documentSnapshot.Uri.LocalPath;
                results.Add(new RazorMappedSpanResult(filePath, linePositionSpan, mappedSpan));
            }

            return results.ToImmutable();
        }

        // Internal for testing use only
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        internal async Task<IEnumerable<(string filePath, LinePositionSpan linePositionSpan, TextSpan span)>> MapSpansAsyncTest(
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
            IEnumerable<TextSpan> spans,
            SourceText sourceTextGenerated,
            SourceText sourceTextRazor)
        {
            var result = await MapSpansAsync(spans, sourceTextGenerated, sourceTextRazor, cancellationToken: default).ConfigureAwait(false);
            return result.Select(mappedResult => (mappedResult.FilePath, mappedResult.LinePositionSpan, mappedResult.Span));
        }
    }
}
