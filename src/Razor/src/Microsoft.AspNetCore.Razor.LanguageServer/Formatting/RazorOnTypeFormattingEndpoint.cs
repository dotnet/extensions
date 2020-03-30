// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class RazorOnTypeFormattingEndpoint : IDocumentOnTypeFormatHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorFormattingService _razorFormattingService;
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;
        private DocumentOnTypeFormattingCapability _capability;

        public RazorOnTypeFormattingEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorFormattingService razorFormattingService,
            IOptionsMonitor<RazorLSPOptions> optionsMonitor)
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

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _razorFormattingService = razorFormattingService;
            _optionsMonitor = optionsMonitor;
        }

        public DocumentOnTypeFormattingRegistrationOptions GetRegistrationOptions()
        {
            return new DocumentOnTypeFormattingRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector,
                FirstTriggerCharacter = ">",
            };
        }

        public async Task<TextEditContainer> Handle(DocumentOnTypeFormattingParams request, CancellationToken cancellationToken)
        {
            if (!_optionsMonitor.CurrentValue.AutoClosingTags)
            {
                // onTypeFormatting is only used for autoClosingTags support for now.
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

            var edits = await _razorFormattingService.FormatOnTypeAsync(request.TextDocument.Uri, codeDocument, request.Position, request.Character, request.Options);

            var editContainer = new TextEditContainer(edits);
            return editContainer;
        }

        public void SetCapability(DocumentOnTypeFormattingCapability capability)
        {
            _capability = capability;
        }
    }
}
