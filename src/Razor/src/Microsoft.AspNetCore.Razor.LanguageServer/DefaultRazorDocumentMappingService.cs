// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultRazorDocumentMappingService : RazorDocumentMappingService
    {
        public override bool TryMapFromProjectedDocumentRange(RazorCodeDocument codeDocument, Range projectedRange, out Range originalRange)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (projectedRange is null)
            {
                throw new ArgumentNullException(nameof(projectedRange));
            }

            originalRange = default;

            var csharpSourceText = SourceText.From(codeDocument.GetCSharpDocument().GeneratedCode);
            var range = projectedRange;
            var startIndex = range.Start.GetAbsoluteIndex(csharpSourceText);
            if (!TryMapFromProjectedDocumentPosition(codeDocument, startIndex, out var hostDocumentStart, out var _))
            {
                return false;
            }

            var endIndex = range.End.GetAbsoluteIndex(csharpSourceText);
            if (!TryMapFromProjectedDocumentPosition(codeDocument, endIndex, out var hostDocumentEnd, out var _))
            {
                return false;
            }

            originalRange = new Range(
                hostDocumentStart,
                hostDocumentEnd);

            return true;
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
            var sourceText = codeDocument.GetSourceText();
            var range = originalRange;

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
                        var generatedSource = SourceText.From(csharpDoc.GeneratedCode);
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
    }
}
