// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageEndpoint : IRazorLanguageQueryHandler
    {
        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorSyntaxFactsService _syntaxFactsService;
        private readonly VSCodeLogger _logger;

        public RazorLanguageEndpoint(
            ForegroundDispatcherShim foregroundDispatcher,
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

            _logger.Log($"Language query request for ({request.Position.Line}, {request.Position.Character}) = {languageKind}");

            return new RazorLanguageQueryResponse()
            {
                Kind = languageKind,
                // TODO: If C# remap to generated document position
                Position = request.Position,
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
    }
}