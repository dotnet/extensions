// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class RazorCompletionEndpoint : ICompletionHandler, ICompletionResolveHandler
    {
        private static readonly Container<string> DirectiveAttributeCommitCharacters = new Container<string>(" ");
        private CompletionCapability _capability;
        private readonly ILogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorCompletionFactsService _completionFactsService;
        private readonly TagHelperCompletionService _tagHelperCompletionService;
        private readonly TagHelperDescriptionFactory _tagHelperDescriptionFactory;
        private static readonly Command RetriggerCompletionCommand = new Command()
        {
            Name = "editor.action.triggerSuggest",
            Title = "Re-trigger completions...",
        };

        public RazorCompletionEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorCompletionFactsService completionFactsService,
            TagHelperCompletionService tagHelperCompletionService,
            TagHelperDescriptionFactory tagHelperDescriptionFactory,
            ILoggerFactory loggerFactory)
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

            if (tagHelperCompletionService == null)
            {
                throw new ArgumentNullException(nameof(tagHelperCompletionService));
            }

            if (tagHelperDescriptionFactory == null)
            {
                throw new ArgumentNullException(nameof(tagHelperDescriptionFactory));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _completionFactsService = completionFactsService;
            _tagHelperCompletionService = tagHelperCompletionService;
            _tagHelperDescriptionFactory = tagHelperDescriptionFactory;
            _logger = loggerFactory.CreateLogger<RazorCompletionEndpoint>();
        }

        public void SetCapability(CompletionCapability capability)
        {
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
            if (codeDocument.IsUnsupported())
            {
                return new CompletionList(isIncomplete: false);
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();

            var sourceText = await document.GetTextAsync();
            var linePosition = new LinePosition((int)request.Position.Line, (int)request.Position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceSpan(hostDocumentIndex, 0);

            var directiveCompletionItems = _completionFactsService.GetCompletionItems(syntaxTree, tagHelperDocumentContext, location);

            _logger.LogTrace($"Found {directiveCompletionItems.Count} directive completion items.");

            var completionItems = new List<CompletionItem>();
            foreach (var razorCompletionItem in directiveCompletionItems)
            {
                if (TryConvert(razorCompletionItem, out var completionItem))
                {
                    completionItems.Add(completionItem);
                }
            }

            var parameterCompletions = completionItems.Where(completionItem => completionItem.TryGetRazorCompletionKind(out var completionKind) && completionKind == RazorCompletionItemKind.DirectiveAttributeParameter);
            if (parameterCompletions.Any())
            {
                // Parameters are present in the completion list, even though TagHelpers are technically valid we shouldn't flood the completion list
                // with non parameter completions. Filter out the rest.
                completionItems = parameterCompletions.ToList();
            }
            else
            {
                var tagHelperCompletionItems = _tagHelperCompletionService.GetCompletionsAt(location, codeDocument);

                _logger.LogTrace($"Found {tagHelperCompletionItems.Count} TagHelper completion items.");

                completionItems.AddRange(tagHelperCompletionItems);
            }

            var completionList = new CompletionList(completionItems, isIncomplete: false);

            return completionList;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector,
                ResolveProvider = true,
                TriggerCharacters = new Container<string>("@", "<", ":"),
            };
        }

        public bool CanResolve(CompletionItem completionItem)
        {
            if (completionItem.TryGetRazorCompletionKind(out var completionItemKind))
            {
                switch (completionItemKind)
                {
                    case RazorCompletionItemKind.DirectiveAttribute:
                    case RazorCompletionItemKind.DirectiveAttributeParameter:
                        return true;
                }

                return false;
            }

            if (completionItem.IsTagHelperElementCompletion() ||
                completionItem.IsTagHelperAttributeCompletion())
            {
                return true;
            }

            return false;
        }

        public Task<CompletionItem> Handle(CompletionItem completionItem, CancellationToken cancellationToken)
        {
            string markdown = null;
            if (completionItem.TryGetRazorCompletionKind(out var completionItemKind))
            {
                switch (completionItemKind)
                {
                    case RazorCompletionItemKind.DirectiveAttribute:
                    case RazorCompletionItemKind.DirectiveAttributeParameter:
                        var descriptionInfo = completionItem.GetAttributeDescriptionInfo();
                        _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out markdown);
                        break;
                }
            }
            else
            {
                if (completionItem.IsTagHelperElementCompletion())
                {
                    var descriptionInfo = completionItem.GetElementDescriptionInfo();
                    _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out markdown);
                }

                if (completionItem.IsTagHelperAttributeCompletion())
                {
                    var descriptionInfo = completionItem.GetTagHelperAttributeDescriptionInfo();
                    _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out markdown);
                }
            }


            if (markdown != null)
            {
                var documentation = new StringOrMarkupContent(
                    new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = markdown,
                    });
                completionItem.Documentation = documentation;
            }

            return Task.FromResult(completionItem);
        }

        // Internal for testing
        internal static bool TryConvert(RazorCompletionItem razorCompletionItem, out CompletionItem completionItem)
        {
            switch (razorCompletionItem.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        // There's not a lot of calculation needed for Directives, go ahead and store the documentation/detail
                        // on the completion item.
                        var descriptionInfo = razorCompletionItem.GetDirectiveCompletionDescription();
                        var directiveCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Detail = descriptionInfo.Description,
                            Documentation = descriptionInfo.Description,
                            Kind = CompletionItemKind.Struct,
                        };

                        if (razorCompletionItem == DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem)
                        {
                            directiveCompletionItem.Command = RetriggerCompletionCommand;
                            directiveCompletionItem.Kind = CompletionItemKind.TypeParameter;
                            directiveCompletionItem.Preselect = true;
                        }

                        directiveCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = directiveCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttribute:
                    {
                        var descriptionInfo = razorCompletionItem.GetAttributeCompletionDescription();

                        var directiveAttributeCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                        };

                        var indexerCompletion = razorCompletionItem.DisplayText.EndsWith("...");
                        if (TryResolveDirectiveAttributeInsertionSnippet(razorCompletionItem.InsertText, indexerCompletion, descriptionInfo, out var snippetText))
                        {
                            directiveAttributeCompletionItem.InsertText = snippetText;
                            directiveAttributeCompletionItem.InsertTextFormat = InsertTextFormat.Snippet;
                        }

                        directiveAttributeCompletionItem.SetDescriptionInfo(descriptionInfo);
                        directiveAttributeCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = directiveAttributeCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttributeParameter:
                    {
                        var descriptionInfo = razorCompletionItem.GetAttributeCompletionDescription();
                        var parameterCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                        };

                        parameterCompletionItem.SetDescriptionInfo(descriptionInfo);
                        parameterCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = parameterCompletionItem;
                        return true;
                    }
            }

            completionItem = null;
            return false;
        }

        private static bool TryResolveDirectiveAttributeInsertionSnippet(
            string insertText,
            bool indexerCompletion,
            AttributeCompletionDescription attributeCompletionDescription,
            out string snippetText)
        {
            const string BoolTypeName = "System.Boolean";
            var attributeInfos = attributeCompletionDescription.DescriptionInfos;

            // Boolean returning bound attribute, auto-complete to just the attribute name.
            if (attributeInfos.All(info => info.ReturnTypeName == BoolTypeName))
            {
                snippetText = null;
                return false;
            }

            if (indexerCompletion)
            {
                // Indexer completion
                snippetText = string.Concat(insertText, "$1=\"$2\"$0");
            }
            else
            {
                snippetText = string.Concat(insertText, "=\"$1\"$0");
            }

            return true;
        }
    }
}
