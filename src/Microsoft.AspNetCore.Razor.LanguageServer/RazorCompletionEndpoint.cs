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
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorCompletionEndpoint : ICompletionHandler
    {
        private CompletionCapability _capability;
        private readonly VSCodeLogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorCompletionFactsService _completionFactsService;

        public RazorCompletionEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorCompletionFactsService completionFactsService,
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

            if (completionFactsService == null)
            {
                throw new ArgumentNullException(nameof(completionFactsService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _completionFactsService = completionFactsService;
            _logger = logger;
        }

        public void SetCapability(CompletionCapability capability)
        {
            _logger.Log("Setting capability");

            _capability = capability;
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.AbsolutePath, out var documentSnapshot);

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await document.GetGeneratedOutputAsync();
            var syntaxTree = codeDocument.GetSyntaxTree();

            var sourceText = await document.GetTextAsync();
            var linePosition = new LinePosition((int)request.Position.Line, (int)request.Position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceSpan(hostDocumentIndex, 0);

            var razorCompletionItems = _completionFactsService.GetCompletionItems(syntaxTree, location);

            _logger.Log($"Found {razorCompletionItems.Count} Razor completion items.");

            var completionItems = new List<CompletionItem>();
            foreach (var razorCompletionItem in razorCompletionItems)
            {
                if (razorCompletionItem.Kind != RazorCompletionItemKind.Directive)
                {
                    // Don't support any other types of completion kinds other than directives.
                    continue;
                }

                var directiveCompletionItem = new CompletionItem()
                {
                    Label = razorCompletionItem.DisplayText,
                    InsertText = razorCompletionItem.InsertText,
                    Detail = razorCompletionItem.Description,
                    Documentation = razorCompletionItem.Description,
                    FilterText = razorCompletionItem.DisplayText,
                    SortText = razorCompletionItem.DisplayText,
                    Kind = CompletionItemKind.Struct,
                };

                completionItems.Add(directiveCompletionItem);
            }

            var completionList = new CompletionList(completionItems, isIncomplete: false);

            return completionList;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = RazorDocument.Selector,
                ResolveProvider = true,
                TriggerCharacters = new Container<string>("@"),
            };
        }
    }
}
