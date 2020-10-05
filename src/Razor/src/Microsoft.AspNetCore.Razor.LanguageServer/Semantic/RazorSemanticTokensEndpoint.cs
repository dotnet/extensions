// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document.Proposals;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class RazorSemanticTokensEndpoint : ISemanticTokensHandler, ISemanticTokensRangeHandler, ISemanticTokensDeltaHandler, IRegistrationExtension
    {
        private SemanticTokensCapability _capability;

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

            return await Handle(request.TextDocument.Uri.GetAbsolutePath(), cancellationToken, range: null);
        }

        public async Task<SemanticTokens> Handle(SemanticTokensRangeParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return await Handle(request.TextDocument, cancellationToken, request.Range);
        }

        public async Task<SemanticTokensFullOrDelta?> Handle(SemanticTokensDeltaParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var documentSnapshot = await TryGetCodeDocumentAsync(request.TextDocument.Uri.GetAbsolutePath(), cancellationToken);
            if (documentSnapshot is null)
            {
                return null;
            }

            var edits = await _semanticTokensInfoService.GetSemanticTokensEditsAsync(documentSnapshot, request.TextDocument, request.PreviousResultId, cancellationToken);

            return edits;
        }

        public SemanticTokensRegistrationOptions GetRegistrationOptions()
        {
            return new SemanticTokensRegistrationOptions
            {
                DocumentSelector = RazorDefaults.Selector,
                Full = new SemanticTokensCapabilityRequestFull
                {
                    Delta = true,
                },
                Legend = RazorSemanticTokensLegend.Instance,
                Range = true,
            };
        }

        public void SetCapability(SemanticTokensCapability capability)
        {
            _capability = capability;
        }

        public RegistrationExtensionResult GetRegistration()
        {
            return new RegistrationExtensionResult(LanguageServerConstants.SemanticTokensProviderName, new LegacySemanticTokensOptions
            {
                DocumentProvider = new SemanticTokensDocumentProviderOptions
                {
                    Edits = true,
                },
                Legend = RazorSemanticTokensLegend.Instance,
                RangeProvider = true,
            });
        }

        private async Task<SemanticTokens> Handle(TextDocumentIdentifier textDocument, CancellationToken cancellationToken, Range range = null)
        {
            var absolutePath = textDocument.Uri.GetAbsolutePath();
            var documentSnapshot = await TryGetCodeDocumentAsync(absolutePath, cancellationToken);
            if (documentSnapshot is null)
            {
                return null;
            }

            var tokens = await _semanticTokensInfoService.GetSemanticTokensAsync(documentSnapshot, textDocument, range, cancellationToken);

            return tokens;
        }

        private async Task<DocumentSnapshot> TryGetCodeDocumentAsync(string absolutePath, CancellationToken cancellationToken)
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

            return document;
        }
    }
}
