// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorCompletionEndpoint : ICompletionHandler
    {
        private CompletionCapability _capability;
        private readonly VSCodeLogger _logger;
        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;

        public RazorCompletionEndpoint(
            ForegroundDispatcherShim foregroundDispatcher,
            DocumentResolver documentResolver,
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

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _logger = logger;
        }

        public void SetCapability(CompletionCapability capability)
        {
            _logger.Log("Setting capability");

            _capability = capability;
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.AbsolutePath, out var documentSnapshot);

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await document.GetGeneratedOutputAsync();
            var syntaxTree = codeDocument.GetSyntaxTree();

            if (!AtDirectiveCompletionPoint(codeDocument.Source, request.Position))
            {
                return new CompletionList();
            }

            var directives = syntaxTree.Options.Directives;
            _logger.Log($"Found {directives.Count} directives. At a valid completion point, providing completion.");

            var completionItems = new List<CompletionItem>();
            foreach (var directive in directives)
            {
                var displayName = directive.DisplayName ?? directive.Directive;
                var directiveCompletionItem = new CompletionItem()
                {
                    Label = displayName,
                    InsertText = directive.Directive,
                    Detail = directive.Description,
                    Documentation = directive.Description,
                    FilterText = displayName,
                    SortText = displayName,
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
            };
        }

        private bool AtDirectiveCompletionPoint(RazorSourceDocument sourceDocument, Position position)
        {
            // HACK, need to be able to go from SyntaxTree => usable line collection
            var charArray = new char[sourceDocument.Length];
            sourceDocument.CopyTo(0, charArray, 0, sourceDocument.Length);

            var lineNumber = 0;
            var lineBuilder = new StringBuilder();
            for (var i = 0; i < charArray.Length; i++)
            {
                if (lineNumber == position.Line)
                {
                    lineBuilder.Append(charArray[i]);
                }

                if (charArray[i] == '\r' && i + 1 < charArray.Length && charArray[i + 1] == '\n')
                {
                    if (lineNumber == position.Line)
                    {
                        // Captured entire line
                        break;
                    }
                    i++;
                    lineNumber++;
                }
            }

            var line = lineBuilder.ToString();
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("@"))
            {
                return false;
            }

            if (trimmedLine.IndexOfAny(new[] { '(', '.', ')', '[', ']', ' ', '\t' }) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}
