// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Interfaces;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class RazorSemanticTokensEndpoint : ISemanticTokensHandler, ISemanticTokensRangeHandler, ISemanticTokensEditHandler, IRegistrationExtension
    {
        private const string SemanticCapability = "semanticTokensProvider";

        private readonly ILogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorSemanticTokensInfoService _semanticTokensInfoService;

        public RazorSemanticTokensEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorSemanticTokensInfoService semanticTokensInfoService,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentResolver is null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (semanticTokensInfoService is null)
            {
                throw new ArgumentNullException(nameof(semanticTokensInfoService));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _semanticTokensInfoService = semanticTokensInfoService;
            _logger = loggerFactory.CreateLogger<RazorSemanticTokensEndpoint>();
        }

        public async Task<SemanticTokens> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return await Handle(request.TextDocument.Uri.AbsolutePath, cancellationToken, range: null);
        }

        public async Task<SemanticTokens> Handle(SemanticTokensRangeParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return await Handle(request.TextDocument.Uri.AbsolutePath, cancellationToken, request.Range);
        }

        public async Task<SemanticTokensOrSemanticTokensEdits?> Handle(SemanticTokensEditParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var codeDocument = await TryGetCodeDocumentAsync(request.TextDocument.Uri.AbsolutePath, cancellationToken);
            if (codeDocument is null)
            {
                return null;
            }

            var edits = _semanticTokensInfoService.GetSemanticTokensEdits(codeDocument, request.PreviousResultId);

            return edits;
        }

        public RegistrationExtensionResult GetRegistration()
        {
            var semanticTokensOptions = new SemanticTokensOptions
            {
                DocumentProvider = new SemanticTokensDocumentProviderOptions
                {
                    Edits = true,
                },
                Legend = SemanticTokensLegend.Instance,
                RangeProvider = true,
            };

            return new RegistrationExtensionResult(SemanticCapability, semanticTokensOptions);
        }

        private async Task<SemanticTokens> Handle(string absolutePath, CancellationToken cancellationToken, Range range = null)
        {
            var codeDocument = await TryGetCodeDocumentAsync(absolutePath, cancellationToken);
            if (codeDocument is null)
            {
                return null;
            }

            var tokens = _semanticTokensInfoService.GetSemanticTokens(codeDocument, range);

            return tokens;
        }

        private async Task<RazorCodeDocument> TryGetCodeDocumentAsync(string absolutePath, CancellationToken cancellationToken)
        {
            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(absolutePath, out var documentSnapshot);

                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (document is null)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            return codeDocument;
        }
    }
}
