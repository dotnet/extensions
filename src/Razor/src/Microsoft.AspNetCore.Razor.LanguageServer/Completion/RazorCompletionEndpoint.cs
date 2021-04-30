// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Tooltip;
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
        private PlatformAgnosticCompletionCapability _capability;
        private readonly ILogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorCompletionFactsService _completionFactsService;
        private readonly TagHelperTooltipFactory _tagHelperTooltipFactory;
        private readonly CompletionListCache _completionListCache;
        private static readonly Command RetriggerCompletionCommand = new Command()
        {
            Name = "editor.action.triggerSuggest",
            Title = "Re-trigger completions...",
        };

        private IReadOnlyList<ExtendedCompletionItemKinds> _supportedItemKinds;

        public RazorCompletionEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorCompletionFactsService completionFactsService,
            TagHelperTooltipFactory tagHelperTooltipFactory,
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

            if (tagHelperTooltipFactory == null)
            {
                throw new ArgumentNullException(nameof(tagHelperTooltipFactory));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _completionFactsService = completionFactsService;
            _tagHelperTooltipFactory = tagHelperTooltipFactory;
            _logger = loggerFactory.CreateLogger<RazorCompletionEndpoint>();
            _completionListCache = new CompletionListCache();
        }

        public void SetCapability(CompletionCapability capability)
        {
            _capability = (PlatformAgnosticCompletionCapability)capability;
            _supportedItemKinds = _capability.CompletionItemKind.ValueSet.Cast<ExtendedCompletionItemKinds>().ToList();
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

        public Task<CompletionItem> Handle(CompletionItem completionItem, CancellationToken cancellationToken)
        {
            MarkupContent tagHelperTooltip = null;

            if (!completionItem.TryGetCompletionListResultId(out var resultId))
            {
                // Couldn't resolve.
                return Task.FromResult(completionItem);
            }

            if (!_completionListCache.TryGet(resultId, out var cachedCompletionItems))
            {
                return Task.FromResult(completionItem);
            }

            var labelQuery = completionItem.Label;
            var associatedRazorCompletion = cachedCompletionItems.FirstOrDefault(completion => string.Equals(labelQuery, completion.DisplayText, StringComparison.Ordinal));
            if (associatedRazorCompletion == null)
            {
                Debug.Fail("Could not find an associated razor completion item. This should never happen since we were able to look up the cached completion list.");
                return Task.FromResult(completionItem);
            }

            switch (associatedRazorCompletion.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        var descriptionInfo = associatedRazorCompletion.GetDirectiveCompletionDescription();
                        completionItem.Documentation = descriptionInfo.Description;
                        break;
                    }
                case RazorCompletionItemKind.MarkupTransition:
                    {
                        var descriptionInfo = associatedRazorCompletion.GetMarkupTransitionCompletionDescription();
                        completionItem.Documentation = descriptionInfo.Description;
                        break;
                    }
                case RazorCompletionItemKind.DirectiveAttribute:
                case RazorCompletionItemKind.DirectiveAttributeParameter:
                case RazorCompletionItemKind.TagHelperAttribute:
                    {
                        var descriptionInfo = associatedRazorCompletion.GetAttributeCompletionDescription();
                        _tagHelperTooltipFactory.TryCreateTooltip(descriptionInfo, out tagHelperTooltip);
                        break;
                    }
                case RazorCompletionItemKind.TagHelperElement:
                    {
                        var descriptionInfo = associatedRazorCompletion.GetTagHelperElementDescriptionInfo();
                        _tagHelperTooltipFactory.TryCreateTooltip(descriptionInfo, out tagHelperTooltip);
                        break;
                    }
            }

            if (tagHelperTooltip != null)
            {
                var documentation = new StringOrMarkupContent(tagHelperTooltip);
                completionItem.Documentation = documentation;
            }

            return Task.FromResult(completionItem);
        }

        // Internal for testing
        internal CompletionList CreateLSPCompletionList(IReadOnlyList<RazorCompletionItem> razorCompletionItems) => CreateLSPCompletionList(razorCompletionItems, _completionListCache, _supportedItemKinds, _capability);

        // Internal for benchmarking and testing
        internal static CompletionList CreateLSPCompletionList(
            IReadOnlyList<RazorCompletionItem> razorCompletionItems,
            CompletionListCache completionListCache,
            IReadOnlyList<ExtendedCompletionItemKinds> supportedItemKinds,
            PlatformAgnosticCompletionCapability completionCapability)
        {
            var resultId = completionListCache.Set(razorCompletionItems);
            var completionItems = new List<CompletionItem>();
            foreach (var razorCompletionItem in razorCompletionItems)
            {
                if (TryConvert(razorCompletionItem, supportedItemKinds, out var completionItem))
                {
                    // The completion items are cached and can be retrieved via this result id to enable the "resolve" completion functionality.
                    completionItem.SetCompletionListResultId(resultId);
                    completionItems.Add(completionItem);
                }
            }

            var completionList = new CompletionList(completionItems, isIncomplete: false);

            // We wrap the pre-existing completion list with an optimized completion list to better control serialization/deserialization
            CompletionList optimizedCompletionList;

            if (completionCapability?.VSCompletionList != null)
            {
                // We're operating in VS, lets make a VS specific optimized completion list

                var vsCompletionList = VSCompletionList.Convert(completionList, completionCapability.VSCompletionList);
                optimizedCompletionList = new OptimizedVSCompletionList(vsCompletionList);
            }
            else
            {
                optimizedCompletionList = new OptimizedCompletionList(completionList);
            }

            return optimizedCompletionList;
        }

        // Internal for testing
        internal static bool TryConvert(
            RazorCompletionItem razorCompletionItem,
            IReadOnlyList<ExtendedCompletionItemKinds> supportedItemKinds,
            out CompletionItem completionItem)
        {
            if (razorCompletionItem is null)
            {
                throw new ArgumentNullException(nameof(razorCompletionItem));
            }

            var tagHelperCompletionItemKind = CompletionItemKind.TypeParameter;
            if (supportedItemKinds?.Contains(ExtendedCompletionItemKinds.TagHelper) == true)
            {
                tagHelperCompletionItemKind = (CompletionItemKind)ExtendedCompletionItemKinds.TagHelper;
            }

            switch (razorCompletionItem.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        var directiveCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Kind = CompletionItemKind.Struct,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            directiveCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        if (razorCompletionItem == DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem)
                        {
                            directiveCompletionItem.Command = RetriggerCompletionCommand;
                            directiveCompletionItem.Kind = tagHelperCompletionItemKind;
                        }

                        completionItem = directiveCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttribute:
                    {
                        var directiveAttributeCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = tagHelperCompletionItemKind,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            directiveAttributeCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        completionItem = directiveAttributeCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.DirectiveAttributeParameter:
                    {
                        var parameterCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = tagHelperCompletionItemKind,
                        };

                        completionItem = parameterCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.MarkupTransition:
                    {
                        var markupTransitionCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Kind = tagHelperCompletionItemKind,
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
                        var tagHelperElementCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = tagHelperCompletionItemKind,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            tagHelperElementCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        completionItem = tagHelperElementCompletionItem;
                        return true;
                    }
                case RazorCompletionItemKind.TagHelperAttribute:
                    {
                        var tagHelperAttributeCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.InsertText,
                            SortText = razorCompletionItem.InsertText,
                            Kind = tagHelperCompletionItemKind,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            tagHelperAttributeCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        completionItem = tagHelperAttributeCompletionItem;
                        return true;
                    }
            }

            completionItem = null;
            return false;
        }
    }
}
