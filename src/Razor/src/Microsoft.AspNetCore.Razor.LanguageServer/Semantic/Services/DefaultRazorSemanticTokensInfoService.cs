// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class DefaultRazorSemanticTokensInfoService : RazorSemanticTokensInfoService
    {
        // This cache is not created for performance, but rather to restrict memory growth.
        // We need to keep track of the last couple of requests for use in previousResultId, but if we let the grow unbounded it could quickly allocate a lot of memory.
        // Solution: an in-memory cache
        private static readonly MemoryCache<IReadOnlyList<uint>> _semanticTokensCache = new MemoryCache<IReadOnlyList<uint>>();

        public override SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument)
        {
            return GetSemanticTokens(codeDocument, range: null);
        }

        public override SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument, Range range)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var semanticRanges = TagHelperSemanticRangeVisitor.VisitAllNodes(codeDocument, range);

            var semanticTokens = ConvertSemanticRangesToSemanticTokens(semanticRanges, codeDocument);

            return semanticTokens;
        }

        public override SemanticTokensOrSemanticTokensEdits GetSemanticTokensEdits(RazorCodeDocument codeDocument, string previousResultId)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (string.IsNullOrEmpty(previousResultId))
            {
                throw new ArgumentException(nameof(previousResultId));
            }

            var semanticRanges = TagHelperSemanticRangeVisitor.VisitAllNodes(codeDocument);

            var previousResults = _semanticTokensCache.Get(previousResultId);
            var newTokens = ConvertSemanticRangesToSemanticTokens(semanticRanges, codeDocument);

            var semanticEdits = SyntaxTokenToSemanticTokensEditHelper.ConvertSyntaxTokensToSemanticEdits(newTokens, previousResults);

            return semanticEdits;
        }

        private static SemanticTokens ConvertSemanticRangesToSemanticTokens(
            IReadOnlyList<SemanticRange> semanticRanges,
            RazorCodeDocument razorCodeDocument)
        {
            if (semanticRanges is null)
            {
                return null;
            }

            SemanticRange previousResult = null;

            var data = new List<uint>();
            foreach (var result in semanticRanges)
            {
                var newData = GetData(result, previousResult, razorCodeDocument);
                data.AddRange(newData);

                previousResult = result;
            }

            var resultId = Guid.NewGuid();

            var tokensResult = new SemanticTokens
            {
                Data = data.ToArray(),
                ResultId = resultId.ToString()
            };

            _semanticTokensCache.Set(resultId.ToString(), data);

            return tokensResult;
        }

        /**
         * In short, each token takes 5 integers to represent, so a specific token `i` in the file consists of the following array indices:
         *  - at index `5*i`   - `deltaLine`: token line number, relative to the previous token
         *  - at index `5*i+1` - `deltaStart`: token start character, relative to the previous token (relative to 0 or the previous token's start if they are on the same line)
         *  - at index `5*i+2` - `length`: the length of the token. A token cannot be multiline.
         *  - at index `5*i+3` - `tokenType`: will be looked up in `SemanticTokensLegend.tokenTypes`
         *  - at index `5*i+4` - `tokenModifiers`: each set bit will be looked up in `SemanticTokensLegend.tokenModifiers`
        **/
        private static IEnumerable<uint> GetData(
            SemanticRange currentRange,
            SemanticRange previousRange,
            RazorCodeDocument razorCodeDocument)
        {
            // var previousRange = previousRange?.Range;
            // var currentRange = currentRange.Range;

            // deltaLine
            var previousLineIndex = previousRange?.Range == null ? 0 : previousRange.Range.Start.Line;
            yield return (uint)(currentRange.Range.Start.Line - previousLineIndex);

            // deltaStart
            if (previousRange != null && previousRange?.Range.Start.Line == currentRange.Range.Start.Line)
            {
                yield return (uint)(currentRange.Range.Start.Character - previousRange.Range.Start.Character);
            }
            else
            {
                yield return (uint)currentRange.Range.Start.Character;
            }

            // length
            var textSpan = currentRange.Range.AsTextSpan(razorCodeDocument.GetSourceText());
            var length = textSpan.Length;
            Debug.Assert(length > 0);
            yield return (uint)length;

            // tokenType
            yield return currentRange.Kind;

            // tokenModifiers
            // We don't currently have any need for tokenModifiers
            yield return 0;
        }
    }
}
