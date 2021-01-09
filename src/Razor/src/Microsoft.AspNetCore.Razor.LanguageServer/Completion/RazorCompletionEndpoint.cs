// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class RazorCompletionEndpoint : ICompletionHandler, ICompletionResolveHandler
    {
        private CompletionCapability _capability;
        private readonly ILogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorCompletionFactsService _completionFactsService;
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
                _documentResolver.TryResolveDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), out var documentSnapshot);

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (document is null || cancellationToken.IsCancellationRequested)
            {
                return new CompletionList(isIncomplete: false);
            }

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

            var razorCompletionItems = _completionFactsService.GetCompletionItems(syntaxTree, tagHelperDocumentContext, location);

            _logger.LogTrace($"Resolved {razorCompletionItems.Count} completion items.");

            var completionList = CreateLSPCompletionList(razorCompletionItems);

            return completionList;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector,
                ResolveProvider = true,
                TriggerCharacters = new Container<string>("@", "<", ":"),

                // NOTE: This property is *NOT* processed in O# versions < 0.16
                // https://github.com/OmniSharp/csharp-language-server-protocol/blame/bdec4c73240be52fbb25a81f6ad7d409f77b5215/src/Protocol/Server/Capabilities/CompletionOptions.cs#L35-L44
                AllCommitCharacters = new Container<string>(":", ">", " ", "="),
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
            MarkupContent tagHelperDescription = null;

            if (completionItem.TryGetRazorCompletionKind(out var completionItemKind))
            {
                switch (completionItemKind)
                {
                    case RazorCompletionItemKind.DirectiveAttribute:
                    case RazorCompletionItemKind.DirectiveAttributeParameter:
                        var descriptionInfo = completionItem.GetAttributeDescriptionInfo();
                        _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out tagHelperDescription);
                        break;
                }
            }
            else
            {
                if (completionItem.IsTagHelperElementCompletion())
                {
                    var descriptionInfo = completionItem.GetElementDescriptionInfo();
                    _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out tagHelperDescription);
                }

                if (completionItem.IsTagHelperAttributeCompletion())
                {
                    var descriptionInfo = completionItem.GetTagHelperAttributeDescriptionInfo();
                    _tagHelperDescriptionFactory.TryCreateDescription(descriptionInfo, out tagHelperDescription);
                }
            }

            if (tagHelperDescription != null)
            {
                var documentation = new StringOrMarkupContent(tagHelperDescription);
                completionItem.Documentation = documentation;
            }

            return Task.FromResult(completionItem);
        }

        // Internal for benchmarking
        internal static CompletionList CreateLSPCompletionList(IReadOnlyList<RazorCompletionItem> razorCompletionItems)
        {
            var completionItems = new List<CompletionItem>();
            foreach (var razorCompletionItem in razorCompletionItems)
            {
                if (TryConvert(razorCompletionItem, out var completionItem))
                {
                    completionItems.Add(completionItem);
                }
            }

            var completionList = new CompletionList(completionItems, isIncomplete: false);

            // We wrap the pre-existing completion list with an optimized completion list to better control serialization/deserialization
            var optimizedCompletionList = new OptimizedCompletionList(completionList);
            return optimizedCompletionList;
        }

        // Internal for testing
        internal static bool TryConvert(RazorCompletionItem razorCompletionItem, out CompletionItem completionItem)
        {
            if (razorCompletionItem is null)
            {
                throw new ArgumentNullException(nameof(razorCompletionItem));
            }

            switch (razorCompletionItem.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        // There's not a lot of calculation needed for Directives, go ahead and store the documentation
                        // on the completion item.
                        var descriptionInfo = razorCompletionItem.GetDirectiveCompletionDescription();
                        var directiveCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Documentation = descriptionInfo.Description,
                            Kind = CompletionItemKind.Struct,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            directiveCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        if (razorCompletionItem == DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem)
                        {
                            directiveCompletionItem.Command = RetriggerCompletionCommand;
                            directiveCompletionItem.Kind = CompletionItemKind.TypeParameter;
                            directiveCompletionItem.Icon = VSLspCompletionItemIcons.TagHelper;
                        }

                        directiveCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = directiveCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttribute:
                    {
                        var descriptionInfo = razorCompletionItem.GetAttributeCompletionDescription();

                        var directiveAttributeCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                            Icon = VSLspCompletionItemIcons.TagHelper,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            directiveAttributeCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        directiveAttributeCompletionItem.SetDescriptionInfo(descriptionInfo);
                        directiveAttributeCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = directiveAttributeCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttributeParameter:
                    {
                        var descriptionInfo = razorCompletionItem.GetAttributeCompletionDescription();
                        var parameterCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                            Icon = VSLspCompletionItemIcons.TagHelper,
                        };

                        parameterCompletionItem.SetDescriptionInfo(descriptionInfo);
                        parameterCompletionItem.SetRazorCompletionKind(razorCompletionItem.Kind);
                        completionItem = parameterCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.MarkupTransition:
                    {
                        var descriptionInfo = razorCompletionItem.GetMarkupTransitionCompletionDescription();
                        var markupTransitionCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Documentation = descriptionInfo.Description,
                            Kind = CompletionItemKind.TypeParameter,
                            Icon = VSLspCompletionItemIcons.TagHelper,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            markupTransitionCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        completionItem = markupTransitionCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.TagHelperElement:
                    {
                        var tagHelperElementCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                            Icon = VSLspCompletionItemIcons.TagHelper,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            tagHelperElementCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        var descriptionInfo = razorCompletionItem.GetTagHelperElementDescriptionInfo();
                        tagHelperElementCompletionItem.SetDescriptionInfo(descriptionInfo);

                        completionItem = tagHelperElementCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.TagHelperAttribute:
                    {
                        var tagHelperAttributeCompletionItem = new VSLspCompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = CompletionItemKind.TypeParameter,
                            Icon = VSLspCompletionItemIcons.TagHelper,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            tagHelperAttributeCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        var descriptionInfo = razorCompletionItem.GetTagHelperAttributeDescriptionInfo();
                        tagHelperAttributeCompletionItem.SetDescriptionInfo(descriptionInfo);

                        completionItem = tagHelperAttributeCompletionItem;
                        return true;
                    }
            }

            completionItem = null;
            return false;
        }
    }
}
