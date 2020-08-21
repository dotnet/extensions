// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class RazorSpanMappingService: IRazorSpanMappingService
    {
        private readonly DocumentSnapshot _document;

        public RazorSpanMappingService(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _document = document;
        }

        public async Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
            Document document, 
            IEnumerable<TextSpan> spans, 
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            // Called on an uninitialized document.
            if (_document == null)
            {
                return ImmutableArray.Create<RazorMappedSpanResult>();
            }

            var source = await _document.GetTextAsync().ConfigureAwait(false);
            var output = await _document.GetGeneratedOutputAsync().ConfigureAwait(false);

            var results = ImmutableArray.CreateBuilder<RazorMappedSpanResult>();
            foreach (var span in spans)
            {
                if (TryGetMappedSpans(span, source, output.GetCSharpDocument(), out var linePositionSpan, out var mappedSpan))
                {
                    results.Add(new RazorMappedSpanResult(output.Source.FilePath, linePositionSpan, mappedSpan));
                }
                else
                {
                    results.Add(default);
                }
            }

            return results.ToImmutable();
        }

        // Internal for testing.
        internal static bool TryGetMappedSpans(TextSpan span, SourceText source, RazorCSharpDocument output, out LinePositionSpan linePositionSpan, out TextSpan mappedSpan)
        {
            var mappings = output.SourceMappings;
            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                var original = mapping.OriginalSpan.AsTextSpan();
                var generated = mapping.GeneratedSpan.AsTextSpan();

                if (!generated.Contains(span))
                {
                    // If the search span isn't contained within the generated span, it is not a match. 
                    // A C# identifier won't cover multiple generated spans.
                    continue;
                }

                var leftOffset = span.Start - generated.Start;
                var rightOffset = span.End - generated.End;
                if (leftOffset >= 0 && rightOffset <= 0)
                {
                    // This span mapping contains the span.
                    mappedSpan = new TextSpan(original.Start + leftOffset, (original.End + rightOffset) - (original.Start + leftOffset));
                    linePositionSpan = source.Lines.GetLinePositionSpan(mappedSpan);
                    return true;
                }
            }

            mappedSpan = default;
            linePositionSpan = default;
            return false;
        }
    }
}
