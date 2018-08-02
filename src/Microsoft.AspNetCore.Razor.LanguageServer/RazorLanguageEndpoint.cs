// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

            var hostDocumentIndex = 0;
            if (codeDocument.Source.Lines.Count == request.Position.Line)
            {
                // Empty newline at end of file, HACKKK
                hostDocumentIndex = codeDocument.Source.Length;
            }
            else
            {
                hostDocumentIndex = codeDocument.Source.Lines.GetLineStart((int)request.Position.Line) + (int)request.Position.Character;
            }

            var classifiedSpans = _syntaxFactsService.GetClassifiedSpans(syntaxTree);
            for (var i = 0; i < classifiedSpans.Count; i++)
            {
                var classifiedSpan = classifiedSpans[i];
                var span = classifiedSpan.Span;

                if (span.AbsoluteIndex <= hostDocumentIndex &&
                    span.AbsoluteIndex + span.Length >= hostDocumentIndex)
                {
                    // Overlaps with request
                    switch (classifiedSpan.SpanKind)
                    {
                        case SpanKind.Markup:
                            _logger.Log($"Language query request for ({request.Position.Line}, {request.Position.Character}) = HTML");

                            // HTML
                            return new RazorLanguageQueryResponse()
                            {
                                Position = request.Position,
                                Kind = LanguageKind.Html,
                            };
                        case SpanKind.Code:
                            _logger.Log($"Language query request for ({request.Position.Line}, {request.Position.Character}) = C#");
                            return new RazorLanguageQueryResponse()
                            {
                                // TODO: REMAP TO C# generated document position
                                Position = request.Position,
                                Kind = LanguageKind.CSharp,
                            };
                    }
                        
                }
            }

            // Couldn't find classified span overlapping request position


            _logger.Log($"Language query request for ({request.Position.Line}, {request.Position.Character}) = Razor");
            return new RazorLanguageQueryResponse()
            {
                Position = request.Position,
                Kind = LanguageKind.Razor
            };
        }
    }
}