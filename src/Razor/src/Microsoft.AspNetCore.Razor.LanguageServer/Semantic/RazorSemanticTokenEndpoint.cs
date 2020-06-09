// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class RazorSemanticTokenEndpoint : ISemanticTokenHandler
    {
        private SemanticTokensCapability _capability;
        private readonly ILogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorSemanticTokenInfoService _semanticTokenInfoService;

        public RazorSemanticTokenEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorSemanticTokenInfoService semanticTokenInfoService,
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

            if (semanticTokenInfoService is null)
            {
                throw new ArgumentNullException(nameof(semanticTokenInfoService));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _semanticTokenInfoService = semanticTokenInfoService;
            _logger = loggerFactory.CreateLogger<RazorSemanticTokenEndpoint>();
        }

        public async Task<SemanticTokens> Handle(SemanticTokenParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.RazorDocumentUri.AbsolutePath, out var documentSnapshot);

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

            var tokens = _semanticTokenInfoService.GetSemanticTokens(codeDocument);

            return tokens;
        }

        public void SetCapability(SemanticTokensCapability capability)
        {
            _capability = capability;
        }
    }
}
