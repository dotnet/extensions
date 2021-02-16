// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class DefaultRazorSemanticTokensInfoService : RazorSemanticTokensInfoService
    {
        // This cache is created to restrict memory growth.
        // We need to keep track of the last couple of requests for use in previousResultId,
        // but if we let the grow unbounded it could quickly allocate a lot of memory.
        // Solution: an in-memory cache
        private readonly MemoryCache<string, (VersionStamp SemanticVersion, IReadOnlyList<int> Data)> _semanticTokensCache =
            new MemoryCache<string, (VersionStamp SemanticVersion, IReadOnlyList<int> Data)>();

        private readonly ClientNotifierServiceBase _languageServer;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly ILogger _logger;
        private readonly RazorDocumentMappingService _documentMappingService;

        public DefaultRazorSemanticTokensInfoService(
            ClientNotifierServiceBase languageServer,
            RazorDocumentMappingService documentMappingService,
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            DocumentVersionCache documentVersionCache,
            ILoggerFactory loggerFactory)
        {
            _languageServer = languageServer ?? throw new ArgumentNullException(nameof(languageServer));
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _documentVersionCache = documentVersionCache ?? throw new ArgumentNullException(nameof(documentVersionCache));

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<DefaultRazorSemanticTokensInfoService>();
        }

        public override Task<SemanticTokens?> GetSemanticTokensAsync(
            TextDocumentIdentifier textDocumentIdentifier,
            CancellationToken cancellationToken)
        {
            return GetSemanticTokensAsync(textDocumentIdentifier, range: null, cancellationToken);
        }

        public override async Task<SemanticTokens?> GetSemanticTokensAsync(
            TextDocumentIdentifier textDocumentIdentifier,
            Range? range,
            CancellationToken cancellationToken)
        {
            var documentPath = textDocumentIdentifier.Uri.GetAbsolutePath();
            if (documentPath is null)
            {
                return null;
            }

            var (documentSnapshot, documentVersion) = await TryGetDocumentInfoAsync(documentPath, cancellationToken);
            if (documentSnapshot is null || documentVersion is null)
            {
                return null;
            }

            var codeDocument = await GetRazorCodeDocumentAsync(documentSnapshot);
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var razorSemanticRanges = TagHelperSemanticRangeVisitor.VisitAllNodes(codeDocument, range);
            IReadOnlyList<SemanticRange>? csharpSemanticRanges = null;

            try
            {
                csharpSemanticRanges = await GetCSharpSemanticRangesAsync(codeDocument, textDocumentIdentifier, range, documentVersion, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown while retrieving CSharp semantic range");
            }

            var combinedSemanticRanges = CombineSemanticRanges(razorSemanticRanges, csharpSemanticRanges);

            // We return null when we have an incomplete view of the document.
            // Likely CSharp ahead of us in terms of document versions.
            // We return null (which to the LSP is a no-op) to prevent flashing of CSharp elements.
            if (combinedSemanticRanges is null)
            {
                return null;
            }

            var semanticVersion = await GetDocumentSemanticVersionAsync(documentSnapshot);

            var razorSemanticTokens = ConvertSemanticRangesToSemanticTokens(combinedSemanticRanges, codeDocument);
            _semanticTokensCache.Set(razorSemanticTokens.ResultId!, (semanticVersion, razorSemanticTokens.Data));

            return razorSemanticTokens;
        }

        public override async Task<SemanticTokensFullOrDelta?> GetSemanticTokensEditsAsync(
            TextDocumentIdentifier textDocumentIdentifier,
            string? previousResultId,
            CancellationToken cancellationToken)
        {
            var documentPath = textDocumentIdentifier.Uri.GetAbsolutePath();
            if (documentPath is null)
            {
                return null;
            }

            var (documentSnapshot, documentVersion) = await TryGetDocumentInfoAsync(documentPath, cancellationToken);
            if (documentSnapshot is null || documentVersion is null)
            {
                return null;
            }

            var codeDocument = await GetRazorCodeDocumentAsync(documentSnapshot);
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }
            cancellationToken.ThrowIfCancellationRequested();

            VersionStamp? cachedSemanticVersion = null;
            IReadOnlyList<int>? previousResults = null;
            if (previousResultId != null)
            {
                _semanticTokensCache.TryGetValue(previousResultId, out var tuple);

                previousResults = tuple.Data;
                cachedSemanticVersion = tuple.SemanticVersion;
            }

            var semanticVersion = await GetDocumentSemanticVersionAsync(documentSnapshot);
            cancellationToken.ThrowIfCancellationRequested();

            // SemanticVersion is different if there's been any text edits to the razor file or ProjectVersion has changed.
            if (semanticVersion == default || cachedSemanticVersion != semanticVersion)
            {
                var razorSemanticRanges = TagHelperSemanticRangeVisitor.VisitAllNodes(codeDocument);
                var csharpSemanticRanges = await GetCSharpSemanticRangesAsync(codeDocument, textDocumentIdentifier, range: null, documentVersion, cancellationToken);

                var combinedSemanticRanges = CombineSemanticRanges(razorSemanticRanges, csharpSemanticRanges);

                // We return null when we have an incomplete view of the document.
                // Likely CSharp ahead of us in terms of document versions.
                // We return null (which to the LSP is a no-op) to prevent flashing of CSharp elements.
                if (combinedSemanticRanges is null)
                {
                    return null;
                }

                var newTokens = ConvertSemanticRangesToSemanticTokens(combinedSemanticRanges, codeDocument);
                _semanticTokensCache.Set(newTokens.ResultId!, (semanticVersion, newTokens.Data));

                if (previousResults is null)
                {
                    return newTokens;
                }

                var razorSemanticEdits = SemanticTokensEditsDiffer.ComputeSemanticTokensEdits(newTokens, previousResults);

                return razorSemanticEdits;
            }
            else
            {
                var result = new SemanticTokensFullOrDelta(new SemanticTokensDelta
                {
                    Edits = Array.Empty<SemanticTokensEdit>(),
                    ResultId = previousResultId,
                });

                return result;
            }
        }

        private async Task<RazorCodeDocument?> GetRazorCodeDocumentAsync(DocumentSnapshot documentSnapshot)
        {
            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            return codeDocument;
        }

        private IReadOnlyList<SemanticRange>? CombineSemanticRanges(params IReadOnlyList<SemanticRange>?[] rangesArray)
        {
            if (rangesArray.Any(a => a is null))
            {
                // If we have an incomplete view of the situation we should return null so we avoid flashing.
                return null;
            }

            var newList = new List<SemanticRange>();
            foreach (var list in rangesArray)
            {
                if (list != null)
                {
                    newList.AddRange(list);
                }
            }

            // Because SemanticToken data is generated relative to the previous token it must be in order.
            // We have a guarentee of order within any given language server, but the interweaving of them can be quite complex.
            // Rather than attempting to reason about transition zones we can simply order our ranges since we know there can be no overlapping range.
            newList.Sort();

            return newList;
        }

        private async Task<IReadOnlyList<SemanticRange>?> GetCSharpSemanticRangesAsync(
            RazorCodeDocument codeDocument,
            TextDocumentIdentifier textDocumentIdentifier,
            Range? range,
            long? documentVersion,
            CancellationToken cancellationToken)
        {
            var csharpResponses = await GetMatchingCSharpResponseAsync(textDocumentIdentifier, documentVersion, cancellationToken);

            if (csharpResponses is null)
            {
                return null;
            }

            SemanticRange? previousSemanticRange = null;
            var razorRanges = new List<SemanticRange>();
            for (var i = 0; i < csharpResponses.Data.Length; i += 5)
            {
                var lineDelta = csharpResponses.Data[i];
                var charDelta = csharpResponses.Data[i + 1];
                var length = csharpResponses.Data[i + 2];
                var tokenType = csharpResponses.Data[i + 3];
                var tokenModifiers = csharpResponses.Data[i + 4];

                var semanticRange = DataToSemanticRange(lineDelta, charDelta, length, tokenType, tokenModifiers, previousSemanticRange);
                if (_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, semanticRange.Range, out var originalRange))
                {
                    var razorRange = new SemanticRange(semanticRange.Kind, originalRange, tokenModifiers);
                    if (range is null || range.OverlapsWith(razorRange.Range))
                    {
                        razorRanges.Add(razorRange);
                    }
                }
                previousSemanticRange = semanticRange;
            }

            var result = razorRanges.ToImmutableList();

            return result;
        }

        private async Task<SemanticTokens?> GetMatchingCSharpResponseAsync(
            TextDocumentIdentifier textDocumentIdentifier,
            long? documentVersion,
            CancellationToken cancellationToken)
        {
            var parameter = new SemanticTokensParams
            {
                TextDocument = textDocumentIdentifier,
            };
            var request = await _languageServer.SendRequestAsync(LanguageServerConstants.RazorProvideSemanticTokensEndpoint, parameter);
            var csharpResponse = await request.Returning<ProvideSemanticTokensResponse>(cancellationToken);

            if (csharpResponse is null ||
                (csharpResponse.HostDocumentSyncVersion != null && csharpResponse.HostDocumentSyncVersion != documentVersion))
            {
                // No C# response or C# is out of sync with us. Unrecoverable, return null to indicate no change. It will retry in a bit.
                return null;
            }

            return csharpResponse.Result;
        }

        private SemanticRange DataToSemanticRange(int lineDelta, int charDelta, int length, int tokenType, int tokenModifiers, SemanticRange? previousSemanticRange = null)
        {
            if (previousSemanticRange is null)
            {
                previousSemanticRange = new SemanticRange(0, new Range(new Position(0, 0), new Position(0, 0)), modifier: 0);
            }

            var startLine = previousSemanticRange.Range.End.Line + lineDelta;
            var previousEndChar = lineDelta == 0 ? previousSemanticRange.Range.Start.Character : 0;
            var startCharacter = previousEndChar + charDelta;
            var start = new Position(startLine, startCharacter);

            var endLine = startLine;
            var endCharacter = startCharacter + length;
            var end = new Position(endLine, endCharacter);

            var range = new Range(start, end);
            var semanticRange = new SemanticRange(tokenType, range, tokenModifiers);

            return semanticRange;
        }

        private SemanticTokens ConvertSemanticRangesToSemanticTokens(
            IReadOnlyList<SemanticRange> semanticRanges,
            RazorCodeDocument razorCodeDocument)
        {
            SemanticRange? previousResult = null;

            var data = new List<int>();
            foreach (var result in semanticRanges)
            {
                var newData = GetData(result, previousResult, razorCodeDocument);
                data.AddRange(newData);

                previousResult = result;
            }

            var resultId = Guid.NewGuid();

            var tokensResult = new SemanticTokens
            {
                Data = data.ToImmutableArray(),
                ResultId = resultId.ToString()
            };

            return tokensResult;
        }

        private static async Task<VersionStamp> GetDocumentSemanticVersionAsync(DocumentSnapshot documentSnapshot)
        {
            var documentVersionStamp = await documentSnapshot.GetTextVersionAsync();
            var semanticVersion = documentVersionStamp.GetNewerVersion(documentSnapshot.Project.Version);

            return semanticVersion;
        }

        /**
         * In short, each token takes 5 integers to represent, so a specific token `i` in the file consists of the following array indices:
         *  - at index `5*i`   - `deltaLine`: token line number, relative to the previous token
         *  - at index `5*i+1` - `deltaStart`: token start character, relative to the previous token (relative to 0 or the previous token's start if they are on the same line)
         *  - at index `5*i+2` - `length`: the length of the token. A token cannot be multiline.
         *  - at index `5*i+3` - `tokenType`: will be looked up in `SemanticTokensLegend.tokenTypes`
         *  - at index `5*i+4` - `tokenModifiers`: each set bit will be looked up in `SemanticTokensLegend.tokenModifiers`
        **/
        private static IEnumerable<int> GetData(
            SemanticRange currentRange,
            SemanticRange? previousRange,
            RazorCodeDocument razorCodeDocument)
        {
            // var previousRange = previousRange?.Range;
            // var currentRange = currentRange.Range;

            // deltaLine
            var previousLineIndex = previousRange?.Range is null ? 0 : previousRange.Range.Start.Line;
            yield return currentRange.Range.Start.Line - previousLineIndex;

            // deltaStart
            if (previousRange != null && previousRange?.Range.Start.Line == currentRange.Range.Start.Line)
            {
                yield return currentRange.Range.Start.Character - previousRange.Range.Start.Character;
            }
            else
            {
                yield return currentRange.Range.Start.Character;
            }

            // length
            var textSpan = currentRange.Range.AsTextSpan(razorCodeDocument.GetSourceText());
            var length = textSpan.Length;
            Debug.Assert(length > 0);
            yield return length;

            // tokenType
            yield return currentRange.Kind;

            // tokenModifiers
            // We don't currently have any need for tokenModifiers
            yield return currentRange.Modifier;
        }

        private async Task<(DocumentSnapshot Snapshot, int? Version)> TryGetDocumentInfoAsync(string absolutePath, CancellationToken cancellationToken)
        {
            var documentInfo = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(absolutePath, out var documentSnapshot);
                _documentVersionCache.TryGetDocumentVersion(documentSnapshot, out var version);

                return (documentSnapshot, version);
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            return documentInfo;
        }
    }
}
