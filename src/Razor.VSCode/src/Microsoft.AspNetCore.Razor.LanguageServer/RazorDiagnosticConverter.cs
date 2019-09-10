// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class RazorDiagnosticConverter
    {
        public static Diagnostic Convert(RazorDiagnostic razorDiagnostic, SourceText sourceText)
        {
            if (razorDiagnostic == null)
            {
                throw new ArgumentNullException(nameof(razorDiagnostic));
            }

            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var diagnostic = new Diagnostic()
            {
                Message = razorDiagnostic.GetMessage(),
                Code = razorDiagnostic.Id,
                Severity = ConvertSeverity(razorDiagnostic.Severity),
                Range = ConvertSpanToRange(razorDiagnostic.Span, sourceText),
            };

            return diagnostic;
        }

        // Internal for testing
        internal static DiagnosticSeverity ConvertSeverity(RazorDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case RazorDiagnosticSeverity.Error:
                    return DiagnosticSeverity.Error;
                default:
                    return DiagnosticSeverity.Information;
            }
        }

        // Internal for testing
        internal static Range ConvertSpanToRange(SourceSpan sourceSpan, SourceText sourceText)
        {
            if (sourceSpan == SourceSpan.Undefined)
            {
                return null;
            }

            var spanStartIndex = NormalizeIndex(sourceSpan.AbsoluteIndex);
            var startPosition = sourceText.Lines.GetLinePosition(spanStartIndex);
            var start = new Position()
            {
                Line = startPosition.Line,
                Character = startPosition.Character,
            };

            var spanEndIndex = NormalizeIndex(sourceSpan.AbsoluteIndex + sourceSpan.Length);
            var endPosition = sourceText.Lines.GetLinePosition(spanEndIndex);
            var end = new Position()
            {
                Line = endPosition.Line,
                Character = endPosition.Character,
            };
            var range = new Range()
            {
                Start = start,
                End = end,
            };

            return range;

            int NormalizeIndex(int index)
            {
                if (index >= sourceText.Length)
                {
                    // Span start index is past the end of the document. Roslyn and VSCode don't support 
                    // virtual positions that don't exist on the document; normalize to the last character.
                    index = sourceText.Length - 1;
                }

                return Math.Max(index, 0);
            }
        }
    }
}
