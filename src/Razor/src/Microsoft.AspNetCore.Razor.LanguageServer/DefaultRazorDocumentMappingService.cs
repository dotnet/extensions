// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultRazorDocumentMappingService : RazorDocumentMappingService
    {
        public override bool TryMapFromProjectedDocumentRange(RazorCodeDocument codeDocument, Range projectedRange, out Range originalRange) => TryMapFromProjectedDocumentRange(codeDocument, projectedRange, MappingBehavior.Strict, out originalRange);

        public override bool TryMapFromProjectedDocumentRange(RazorCodeDocument codeDocument, Range projectedRange, MappingBehavior mappingBehavior, out Range originalRange)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (projectedRange is null)
            {
                throw new ArgumentNullException(nameof(projectedRange));
            }

            if (mappingBehavior == MappingBehavior.Strict)
            {
                return TryMapFromProjectedDocumentRangeStrict(codeDocument, projectedRange, out originalRange);
            }
            else if (mappingBehavior == MappingBehavior.Inclusive)
            {
                return TryMapFromProjectedDocumentRangeInclusive(codeDocument, projectedRange, out originalRange);
            }
            else
            {
                throw new InvalidOperationException("Unknown mapping behavior");
            }
        }

        public override bool TryMapToProjectedDocumentRange(RazorCodeDocument codeDocument, Range originalRange, out Range projectedRange)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (originalRange is null)
            {
                throw new ArgumentNullException(nameof(originalRange));
            }

            projectedRange = default;

            if ((originalRange.End.Line < originalRange.Start.Line) ||
                (originalRange.End.Line == originalRange.Start.Line &&
                 originalRange.End.Character < originalRange.Start.Character))
            {
                Debug.Fail($"DefaultRazorDocumentMappingService:TryMapToProjectedDocumentRange original range end < start '{originalRange}'");
                return false;
            }

            var sourceText = codeDocument.GetSourceText();
            var range = originalRange;
            if (!IsRangeWithinDocument(range, sourceText))
            {
                return false;
            }

            var startIndex = range.Start.GetAbsoluteIndex(sourceText);
            if (!TryMapToProjectedDocumentPosition(codeDocument, startIndex, out var projectedStart, out var _))
            {
                return false;
            }

            var endIndex = range.End.GetAbsoluteIndex(sourceText);
            if (!TryMapToProjectedDocumentPosition(codeDocument, endIndex, out var projectedEnd, out var _))
            {
                return false;
            }

            // Ensures a valid range is returned.
            // As we're doing two seperate TryMapToProjectedDocumentPosition calls,
            // it's possible the projectedStart and projectedEnd positions are in completely
            // different places in the document, including the possibility that the
            // projectedEnd position occurs before the projectedStart position.
            // We explicitly disallow such ranges where the end < start.
            if ((projectedEnd.Line < projectedStart.Line) ||
                (projectedEnd.Line == projectedStart.Line &&
                 projectedEnd.Character < projectedStart.Character))
            {
                return false;
            }

            projectedRange = new Range(
                projectedStart,
                projectedEnd);

            return true;
        }

        public override bool TryMapFromProjectedDocumentPosition(RazorCodeDocument codeDocument, int csharpAbsoluteIndex, out Position originalPosition, out int originalIndex)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var csharpDoc = codeDocument.GetCSharpDocument();
            foreach (var mapping in csharpDoc.SourceMappings)
            {
                var generatedSpan = mapping.GeneratedSpan;
                var generatedAbsoluteIndex = generatedSpan.AbsoluteIndex;
                if (generatedAbsoluteIndex <= csharpAbsoluteIndex)
                {
                    // Treat the mapping as owning the edge at its end (hence <= originalSpan.Length),
                    // otherwise we wouldn't handle the cursor being right after the final C# char
                    var distanceIntoGeneratedSpan = csharpAbsoluteIndex - generatedAbsoluteIndex;
                    if (distanceIntoGeneratedSpan <= generatedSpan.Length)
                    {
                        // Found the generated span that contains the csharp absolute index

                        originalIndex = mapping.OriginalSpan.AbsoluteIndex + distanceIntoGeneratedSpan;
                        var originalLocation = codeDocument.Source.Lines.GetLocation(originalIndex);
                        originalPosition = new Position(originalLocation.LineIndex, originalLocation.CharacterIndex);
                        return true;
                    }
                }
            }

            originalPosition = default;
            originalIndex = default;
            return false;
        }

        public override bool TryMapToProjectedDocumentPosition(RazorCodeDocument codeDocument, int absoluteIndex, out Position projectedPosition, out int projectedIndex)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var csharpDoc = codeDocument.GetCSharpDocument();
            foreach (var mapping in csharpDoc.SourceMappings)
            {
                var originalSpan = mapping.OriginalSpan;
                var originalAbsoluteIndex = originalSpan.AbsoluteIndex;
                if (originalAbsoluteIndex <= absoluteIndex)
                {
                    // Treat the mapping as owning the edge at its end (hence <= originalSpan.Length),
                    // otherwise we wouldn't handle the cursor being right after the final C# char
                    var distanceIntoOriginalSpan = absoluteIndex - originalAbsoluteIndex;
                    if (distanceIntoOriginalSpan <= originalSpan.Length)
                    {
                        var generatedSource = codeDocument.GetCSharpSourceText();
                        projectedIndex = mapping.GeneratedSpan.AbsoluteIndex + distanceIntoOriginalSpan;
                        var generatedLinePosition = generatedSource.Lines.GetLinePosition(projectedIndex);
                        projectedPosition = new Position(generatedLinePosition.Line, generatedLinePosition.Character);
                        return true;
                    }
                }
            }

            projectedPosition = default;
            projectedIndex = default;
            return false;
        }

        public override RazorLanguageKind GetLanguageKind(RazorCodeDocument codeDocument, int originalIndex)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = syntaxTree.GetClassifiedSpans();
            var tagHelperSpans = syntaxTree.GetTagHelperSpans();
            var documentLength = codeDocument.GetSourceText().Length;
            var languageKind = GetLanguageKindCore(classifiedSpans, tagHelperSpans, originalIndex, documentLength);

            return languageKind;
        }

        // Internal for testing
        internal static RazorLanguageKind GetLanguageKindCore(
            IReadOnlyList<ClassifiedSpanInternal> classifiedSpans,
            IReadOnlyList<TagHelperSpanInternal> tagHelperSpans,
            int absoluteIndex,
            int documentLength)
        {
            for (var i = 0; i < classifiedSpans.Count; i++)
            {
                var classifiedSpan = classifiedSpans[i];
                var span = classifiedSpan.Span;

                if (span.AbsoluteIndex <= absoluteIndex)
                {
                    var end = span.AbsoluteIndex + span.Length;
                    if (end >= absoluteIndex)
                    {
                        if (end == absoluteIndex)
                        {
                            // We're at an edge.

                            if (span.Length > 0 &&
                                classifiedSpan.AcceptedCharacters == AcceptedCharactersInternal.None)
                            {
                                // Non-marker spans do not own the edges after it
                                continue;
                            }
                        }

                        return GetLanguageFromClassifiedSpan(classifiedSpan);
                    }
                }
            }

            for (var i = 0; i < tagHelperSpans.Count; i++)
            {
                var tagHelperSpan = tagHelperSpans[i];
                var span = tagHelperSpan.Span;

                if (span.AbsoluteIndex <= absoluteIndex)
                {
                    var end = span.AbsoluteIndex + span.Length;
                    if (end >= absoluteIndex)
                    {
                        if (end == absoluteIndex)
                        {
                            // We're at an edge. TagHelper spans never own their edge and aren't represented by marker spans
                            continue;
                        }

                        // Found intersection
                        return RazorLanguageKind.Html;
                    }
                }
            }

            // Use the language of the last classified span if we're at the end
            // of the document.
            if (classifiedSpans.Count != 0 && absoluteIndex == documentLength)
            {
                var lastClassifiedSpan = classifiedSpans.Last();
                return GetLanguageFromClassifiedSpan(lastClassifiedSpan);
            }

            // Default to Razor
            return RazorLanguageKind.Razor;

            static RazorLanguageKind GetLanguageFromClassifiedSpan(ClassifiedSpanInternal classifiedSpan)
            {
                // Overlaps with request
                return classifiedSpan.SpanKind switch
                {
                    SpanKindInternal.Markup => RazorLanguageKind.Html,
                    SpanKindInternal.Code => RazorLanguageKind.CSharp,

                    // Content type was non-C# or Html or we couldn't find a classified span overlapping the request position.
                    // All other classified span kinds default back to Razor
                    _ => RazorLanguageKind.Razor,
                };
            }
        }

        private bool TryMapFromProjectedDocumentRangeStrict(RazorCodeDocument codeDocument, Range projectedRange, out Range originalRange)
        {
            originalRange = default;

            var csharpSourceText = codeDocument.GetCSharpSourceText();
            var range = projectedRange;
            if (!IsRangeWithinDocument(range, csharpSourceText))
            {
                return false;
            }

            var startIndex = range.Start.GetAbsoluteIndex(csharpSourceText);
            if (!TryMapFromProjectedDocumentPosition(codeDocument, startIndex, out var hostDocumentStart, out _))
            {
                return false;
            }

            var endIndex = range.End.GetAbsoluteIndex(csharpSourceText);
            if (!TryMapFromProjectedDocumentPosition(codeDocument, endIndex, out var hostDocumentEnd, out _))
            {
                return false;
            }

            originalRange = new Range(
                hostDocumentStart,
                hostDocumentEnd);

            return true;
        }

        private bool TryMapFromProjectedDocumentRangeInclusive(RazorCodeDocument codeDocument, Range projectedRange, out Range originalRange)
        {
            originalRange = default;

            var csharpDoc = codeDocument.GetCSharpDocument();
            var csharpSourceText = codeDocument.GetCSharpSourceText();
            var projectedRangeAsSpan = projectedRange.AsTextSpan(csharpSourceText);
            var range = projectedRange;
            var startIndex = projectedRangeAsSpan.Start;
            var startMappedDirectly = TryMapFromProjectedDocumentPosition(codeDocument, startIndex, out var hostDocumentStart, out _);

            var endIndex = projectedRangeAsSpan.End;
            var endMappedDirectly = TryMapFromProjectedDocumentPosition(codeDocument, endIndex, out var hostDocumentEnd, out _);

            if (startMappedDirectly && endMappedDirectly)
            {
                // We strictly mapped the start/end of the projected range.
                originalRange = new Range(hostDocumentStart, hostDocumentEnd);
                return true;
            }

            List<SourceMapping> candidateMappings;
            if (startMappedDirectly)
            {
                // Start of projected range intersects with a mapping
                candidateMappings = csharpDoc.SourceMappings.Where(mapping => IntersectsWith(startIndex, mapping.GeneratedSpan)).ToList();
            }
            else if (endMappedDirectly)
            {
                // End of projected range intersects with a mapping
                candidateMappings = csharpDoc.SourceMappings.Where(mapping => IntersectsWith(endIndex, mapping.GeneratedSpan)).ToList();
            }
            else
            {
                // Our range does not intersect with any mapping; we should see if it overlaps generated locations
                candidateMappings = csharpDoc.SourceMappings.Where(mapping => Overlaps(projectedRangeAsSpan, mapping.GeneratedSpan)).ToList();
            }

            if (candidateMappings.Count == 1)
            {
                // We're intersecting or overlapping a single mapping, lets choose that.

                var mapping = candidateMappings[0];
                originalRange = ConvertMapping(codeDocument.Source, mapping);
                return true;
            }
            else
            {
                // More then 1 or exactly 0 intersecting/overlapping mappings
                return false;
            }

            bool Overlaps(TextSpan projectedRangeAsSpan, SourceSpan span)
            {
                var overlapStart = Math.Max(projectedRangeAsSpan.Start, span.AbsoluteIndex);
                var overlapEnd = Math.Min(projectedRangeAsSpan.End, span.AbsoluteIndex + span.Length);

                return overlapStart < overlapEnd;
            }

            bool IntersectsWith(int position, SourceSpan span)
            {
                return unchecked((uint)(position - span.AbsoluteIndex) <= (uint)span.Length);
            }

            static Range ConvertMapping(RazorSourceDocument sourceDocument, SourceMapping mapping)
            {
                var startLocation = sourceDocument.Lines.GetLocation(mapping.OriginalSpan.AbsoluteIndex);
                var endLocation = sourceDocument.Lines.GetLocation(mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length);
                var convertedRange = new Range(
                    new Position(startLocation.LineIndex, startLocation.CharacterIndex),
                    new Position(endLocation.LineIndex, endLocation.CharacterIndex));
                return convertedRange;
            }
        }

        private static bool IsRangeWithinDocument(Range range, SourceText sourceText)
        {
            // This might happen when the document that ranges were created against was not the same as the document we're consulting.
            var result = IsPositionWithinDocument(range.Start, sourceText) && IsPositionWithinDocument(range.End, sourceText);

            Debug.Assert(result, $"Attempted to map a range {range} outside of the Source (line count {sourceText.Lines.Count}.) This could happen if the Roslyn and Razor LSP servers are not in sync.");

            return result;

            static bool IsPositionWithinDocument(Position position, SourceText sourceText)
            {
                return position.Line < sourceText.Lines.Count;
            }
        }
    }
}
