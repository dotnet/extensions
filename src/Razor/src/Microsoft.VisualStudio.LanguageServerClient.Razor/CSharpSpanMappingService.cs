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
    internal sealed class CSharpSpanMappingService : IRazorSpanMappingService
    {
        private readonly LSPDocumentMappingProvider _lspDocumentMappingProvider;

        private readonly ITextSnapshot _textSnapshot;
        private readonly LSPDocumentSnapshot _documentSnapshot;

        public CSharpSpanMappingService(
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
            return await MapSpansAsync(document, spans, _textSnapshot.AsText(), _documentSnapshot.Snapshot.AsText(), cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
            Document document,
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

            var results = ImmutableArray.CreateBuilder<RazorMappedSpanResult>();

            if (mappedResult is null)
            {
                return results.ToImmutable();
            }

            foreach (var mappedRange in mappedResult.Ranges)
            {
                var mappedSpan = mappedRange.AsTextSpan(sourceTextRazor);
                var linePositionSpan = sourceTextRazor.Lines.GetLinePositionSpan(mappedSpan);
                var filePath = _documentSnapshot.Uri.LocalPath;
                results.Add(new RazorMappedSpanResult(filePath, linePositionSpan, mappedSpan));
            }

            return results.ToImmutable();
        }

        // Internal for testing use only
        internal async Task<IEnumerable<(string filePath, LinePositionSpan linePositionSpan, TextSpan span)>> MapSpansAsyncTest(
            IEnumerable<TextSpan> spans,
            SourceText sourceTextGenerated,
            SourceText sourceTextRazor)
        {
            var result = await MapSpansAsync(document: null, spans, sourceTextGenerated, sourceTextRazor, cancellationToken: default).ConfigureAwait(false);
            return result.Select(mappedResult => (mappedResult.FilePath, mappedResult.LinePositionSpan, mappedResult.Span));
        }
    }
}
