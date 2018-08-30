// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageEndpoint : IRazorLanguageQueryHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorSyntaxFactsService _syntaxFactsService;
        private readonly VSCodeLogger _logger;

        public RazorLanguageEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorSyntaxFactsService syntaxFactsService,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (syntaxFactsService == null)
            {
                throw new ArgumentNullException(nameof(syntaxFactsService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _syntaxFactsService = syntaxFactsService;
            _logger = logger;
        }

        public async Task<RazorLanguageQueryResponse> Handle(RazorLanguageQueryParams request, CancellationToken cancellationToken)
        {
            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.Uri.AbsolutePath, out var documentSnapshot);

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await document.GetGeneratedOutputAsync();
            var syntaxTree = codeDocument.GetSyntaxTree();
            var hostDocumentIndex = codeDocument.Source.GetAbsoluteIndex(request.Position);

            var classifiedSpans = _syntaxFactsService.GetClassifiedSpans(syntaxTree);
            var languageKind = GetLanguageKind(classifiedSpans, hostDocumentIndex);

            var responsePosition = request.Position;

            if (languageKind == RazorLanguageKind.CSharp)
            {
                if (TryGetCSharpProjectedPosition(codeDocument, hostDocumentIndex, out var projectedPosition))
                {
                    // For C# locations, we attempt to return the corresponding position
                    // within the projected document
                    responsePosition = projectedPosition;
                }
                else
                {
                    // It no longer makes sense to think of this location as C#, since it doesn't
                    // correspond to any position in the projected document. This should not happen
                    // since there should be source mappings for all the C# spans.
                    languageKind = RazorLanguageKind.Razor;
                }
            }

            _logger.Log($"Language query request for ({request.Position.Line}, {request.Position.Character}) = {languageKind} at ({responsePosition.Line}, {responsePosition.Character})");

            return new RazorLanguageQueryResponse()
            {
                Kind = languageKind,
                Position = responsePosition,
            };
        }

        // Internal for testing
        internal static RazorLanguageKind GetLanguageKind(IReadOnlyList<ClassifiedSpan> classifiedSpans, int absoluteIndex)
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

                            if (classifiedSpan.AcceptedCharacters == AcceptedCharacters.None)
                            {
                                // This span doesn't own the edge after it
                                continue;
                            }
                        }

                        // Overlaps with request
                        switch (classifiedSpan.SpanKind)
                        {
                            case SpanKind.Markup:
                                return RazorLanguageKind.Html;
                            case SpanKind.Code:
                                return RazorLanguageKind.CSharp;
                        }

                        break;
                    }
                }
            }

            // Content type was non-C# or Html or we couldn't find a classified span overlapping the request position.
            // Default to Razor
            return RazorLanguageKind.Razor;
        }

        // Internal for testing
        internal static bool TryGetCSharpProjectedPosition(RazorCodeDocument codeDocument, int absoluteIndex, out Position projectedPosition)
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
                        var generatedAbsoluteIndex = mapping.GeneratedSpan.AbsoluteIndex + distanceIntoOriginalSpan;
                        var generatedLinePosition = generatedSource.Lines.GetLinePosition(generatedAbsoluteIndex);
                        projectedPosition = new Position(generatedLinePosition.Line, generatedLinePosition.Character);
                        return true;
                    }
                }
            }

            projectedPosition = default;
            return false;
        }
    }
}