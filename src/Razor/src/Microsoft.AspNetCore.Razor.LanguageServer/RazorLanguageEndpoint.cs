// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageEndpoint : IRazorLanguageQueryHandler, IRazorMapToDocumentRangeHandler
    {
        private static readonly Range UndefinedRange = new Range(
            start: new Position(-1, -1),
            end: new Position(-1, -1));

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly ILogger _logger;

        public RazorLanguageEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            DocumentVersionCache documentVersionCache,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _documentVersionCache = documentVersionCache;
            _logger = loggerFactory.CreateLogger<RazorLanguageEndpoint>();
        }

        public async Task<RazorLanguageQueryResponse> Handle(RazorLanguageQueryParams request, CancellationToken cancellationToken)
        {
            long documentVersion = -1;
            DocumentSnapshot documentSnapshot = null;
            await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.Uri.AbsolutePath, out documentSnapshot);
                if (!_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out documentVersion))
                {
                    Debug.Fail("Document should always be available here.");
                }

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            var sourceText = await documentSnapshot.GetTextAsync();
            var linePosition = new LinePosition((int)request.Position.Line, (int)request.Position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var responsePosition = request.Position;

            if (codeDocument.IsUnsupported())
            {
                // All language queries on unsupported documents return Html. This is equivalent to what pre-VSCode Razor was capable of.
                return new RazorLanguageQueryResponse()
                {
                    Kind = RazorLanguageKind.Html,
                    Position = responsePosition,
                    PositionIndex = hostDocumentIndex,
                    HostDocumentVersion = documentVersion,
                };
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = syntaxTree.GetClassifiedSpans();
            var tagHelperSpans = syntaxTree.GetTagHelperSpans();
            var languageKind = GetLanguageKind(classifiedSpans, tagHelperSpans, hostDocumentIndex);

            var responsePositionIndex = hostDocumentIndex;

            if (languageKind == RazorLanguageKind.CSharp)
            {
                if (TryGetCSharpProjectedPosition(codeDocument, hostDocumentIndex, out var projectedPosition, out var projectedIndex))
                {
                    // For C# locations, we attempt to return the corresponding position
                    // within the projected document
                    responsePosition = projectedPosition;
                    responsePositionIndex = projectedIndex;
                }
                else
                {
                    // It no longer makes sense to think of this location as C#, since it doesn't
                    // correspond to any position in the projected document. This should not happen
                    // since there should be source mappings for all the C# spans.
                    languageKind = RazorLanguageKind.Razor;
                    responsePositionIndex = hostDocumentIndex;
                }
            }

            _logger.LogTrace($"Language query request for ({request.Position.Line}, {request.Position.Character}) = {languageKind} at ({responsePosition.Line}, {responsePosition.Character})");

            return new RazorLanguageQueryResponse()
            {
                Kind = languageKind,
                Position = responsePosition,
                PositionIndex = responsePositionIndex,
                HostDocumentVersion = documentVersion
            };
        }

        public async Task<RazorMapToDocumentRangeResponse> Handle(RazorMapToDocumentRangeParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            long documentVersion = -1;
            DocumentSnapshot documentSnapshot = null;
            await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.RazorDocumentUri.AbsolutePath, out documentSnapshot);
                if (!_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out documentVersion))
                {
                    Debug.Fail("Document should always be available here.");
                }

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (request.Kind != RazorLanguageKind.CSharp)
            {
                // All other non-C# requests map directly to where they are in the document.
                return new RazorMapToDocumentRangeResponse()
                {
                    Range = request.ProjectedRange,
                    HostDocumentVersion = documentVersion,
                };
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                // All maping requests on unsupported documents return undefined ranges. This is equivalent to what pre-VSCode Razor was capable of.
                return new RazorMapToDocumentRangeResponse()
                {
                    Range = UndefinedRange,
                    HostDocumentVersion = documentVersion,
                };
            }

            var csharpSourceText = SourceText.From(codeDocument.GetCSharpDocument().GeneratedCode);
            var range = request.ProjectedRange;
            var startPosition = range.Start;
            var lineStartPosition = new LinePosition((int)startPosition.Line, (int)startPosition.Character);
            var startIndex = csharpSourceText.Lines.GetPosition(lineStartPosition);
            if (!TryGetHostDocumentPosition(codeDocument, startIndex, out var hostDocumentStart))
            {
                return new RazorMapToDocumentRangeResponse()
                {
                    Range = UndefinedRange,
                    HostDocumentVersion = documentVersion,
                };
            }

            var endPosition = range.End;
            var lineEndPosition = new LinePosition((int)endPosition.Line, (int)endPosition.Character);
            var endIndex = csharpSourceText.Lines.GetPosition(lineEndPosition);
            if (!TryGetHostDocumentPosition(codeDocument, endIndex, out var hostDocumentEnd))
            {
                return new RazorMapToDocumentRangeResponse()
                {
                    Range = UndefinedRange,
                    HostDocumentVersion = documentVersion,
                };
            }

            var remappedDocumentRange = new Range(
                hostDocumentStart,
                hostDocumentEnd);

            return new RazorMapToDocumentRangeResponse()
            {
                Range = remappedDocumentRange,
                HostDocumentVersion = documentVersion,
            };
        }

        // Internal for testing
        internal static RazorLanguageKind GetLanguageKind(
            IReadOnlyList<ClassifiedSpanInternal> classifiedSpans,
            IReadOnlyList<TagHelperSpanInternal> tagHelperSpans,
            int absoluteIndex)
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

                        // Overlaps with request
                        switch (classifiedSpan.SpanKind)
                        {
                            case SpanKindInternal.Markup:
                                return RazorLanguageKind.Html;
                            case SpanKindInternal.Code:
                                return RazorLanguageKind.CSharp;
                        }

                        // Content type was non-C# or Html or we couldn't find a classified span overlapping the request position.
                        // All other classified span kinds default back to Razor
                        return RazorLanguageKind.Razor;
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

            // Default to Razor
            return RazorLanguageKind.Razor;
        }

        // Internal for testing
        internal static bool TryGetCSharpProjectedPosition(RazorCodeDocument codeDocument, int absoluteIndex, out Position projectedPosition, out int projectedIndex)
        {
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

        // Internal for testing
        internal static bool TryGetHostDocumentPosition(RazorCodeDocument codeDocument, int csharpAbsoluteIndex, out Position hostDocumentPosition)
        {
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

                        var hostDocumentIndex = mapping.OriginalSpan.AbsoluteIndex + distanceIntoGeneratedSpan;
                        var originalLocation = codeDocument.Source.Lines.GetLocation(hostDocumentIndex);
                        hostDocumentPosition = new Position(originalLocation.LineIndex, originalLocation.CharacterIndex);
                        return true;
                    }
                }
            }

            hostDocumentPosition = default;
            return false;
        }
    }
}