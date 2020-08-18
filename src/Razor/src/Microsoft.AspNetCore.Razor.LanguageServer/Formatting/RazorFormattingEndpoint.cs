// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class RazorFormattingEndpoint : IDocumentFormattingHandler, IDocumentRangeFormattingHandler
    {
        private DocumentFormattingCapability _formattingCapability;
        private DocumentRangeFormattingCapability _rangeFormattingCapability;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorFormattingService _razorFormattingService;
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;
        private readonly ILogger _logger;

        public RazorFormattingEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorFormattingService razorFormattingService,
            IOptionsMonitor<RazorLSPOptions> optionsMonitor,
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

            if (razorFormattingService is null)
            {
                throw new ArgumentNullException(nameof(razorFormattingService));
            }

            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _razorFormattingService = razorFormattingService;
            _optionsMonitor = optionsMonitor;
            _logger = loggerFactory.CreateLogger<RazorFormattingEndpoint>();
        }

        public TextDocumentRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector,
            };
        }

        public async Task<TextEditContainer> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
        {
            if (!_optionsMonitor.CurrentValue.EnableFormatting)
            {
                return null;
            }

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), out var documentSnapshot);

                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (document is null || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var span = TextSpan.FromBounds(0, codeDocument.Source.Length);
            var range = span.AsRange(codeDocument.GetSourceText());
            var edits = await _razorFormattingService.FormatAsync(request.TextDocument.Uri, document, range, request.Options);

            var editContainer = new TextEditContainer(edits);
            return editContainer;
        }

        public async Task<TextEditContainer> Handle(DocumentRangeFormattingParams request, CancellationToken cancellationToken)
        {
            if (!_optionsMonitor.CurrentValue.EnableFormatting)
            {
                return null;
            }

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), out var documentSnapshot);

                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (document is null || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var edits = await _razorFormattingService.FormatAsync(request.TextDocument.Uri, document, request.Range, request.Options);

            var editContainer = new TextEditContainer(edits);
            return editContainer;
        }

        public void SetCapability(DocumentFormattingCapability capability)
        {
            _formattingCapability = capability;
        }

        public void SetCapability(DocumentRangeFormattingCapability capability)
        {
            _rangeFormattingCapability = capability;
        }
    }
}
