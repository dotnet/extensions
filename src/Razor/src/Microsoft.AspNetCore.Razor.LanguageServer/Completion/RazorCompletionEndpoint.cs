// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        private const int XMLAttributeId = 3564;
        private const string ImageCatalogGuidString = "{ae27a6b0-e345-4288-96df-5eaf394ee369}";
        private static Guid ImageCatalogGuid = new Guid(ImageCatalogGuidString);

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

            // Temporary: We want to set custom icons in VS. Ideally this should be done on the client.
            // This is a workaround until we have support for it in the middle layer.
            var completionItemsWithIcon = completionItems.Select(c => SetIcon(c));

            var completionList = new CompletionList(completionItemsWithIcon, isIncomplete: false);

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
                AllCommitCharacters = new Container<string>(":", ">", " ", "=" ),
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

        // Internal for testing
        internal bool TryConvert(RazorCompletionItem razorCompletionItem, out CompletionItem completionItem)
        {
            switch (razorCompletionItem.Kind)
            {
                case RazorCompletionItemKind.Directive:
                    {
                        // There's not a lot of calculation needed for Directives, go ahead and store the documentation
                        // on the completion item.
                        var descriptionInfo = razorCompletionItem.GetDirectiveCompletionDescription();
                        var directiveCompletionItem = new CompletionItem()
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
                case RazorCompletionItemKind.MarkupTransition:
                    {
                        var descriptionInfo = razorCompletionItem.GetMarkupTransitionCompletionDescription();
                        var markupTransitionCompletionItem = new CompletionItem()
                        {
                            Label = razorCompletionItem.DisplayText,
                            InsertText = razorCompletionItem.InsertText,
                            FilterText = razorCompletionItem.DisplayText,
                            SortText = razorCompletionItem.DisplayText,
                            Documentation = descriptionInfo.Description,
                            Kind = CompletionItemKind.TypeParameter,
                        };

                        if (razorCompletionItem.CommitCharacters != null && razorCompletionItem.CommitCharacters.Count > 0)
                        {
                            markupTransitionCompletionItem.CommitCharacters = new Container<string>(razorCompletionItem.CommitCharacters);
                        }

                        completionItem = markupTransitionCompletionItem;
                        return true;
                    }
            }

            completionItem = null;
            return false;
        }

        private CompletionItem SetIcon(CompletionItem item)
        {
            PascalCasedSerializableImageElement? icon = null;
            if (item.IsTagHelperElementCompletion() || item.IsTagHelperAttributeCompletion())
            {
                icon = new PascalCasedSerializableImageElement(new PascalCasedSerializableImageId(ImageCatalogGuid, XMLAttributeId), automationName: null);
            }
            else if (item.TryGetRazorCompletionKind(out var kind) &&
                (kind == RazorCompletionItemKind.DirectiveAttribute ||
                kind == RazorCompletionItemKind.DirectiveAttributeParameter ||
                kind == RazorCompletionItemKind.MarkupTransition))
            {
                icon = new PascalCasedSerializableImageElement(new PascalCasedSerializableImageId(ImageCatalogGuid, XMLAttributeId), automationName: null);
            }

            if (!icon.HasValue)
            {
                return item;
            }

            return new VSLspCompletionItem()
            {
                Label = item.Label,
                Kind = item.Kind,
                Detail = item.Detail,
                Documentation = item.Documentation,
                Preselect = item.Preselect,
                SortText = item.SortText,
                FilterText = item.FilterText,
                InsertText = item.InsertText,
                InsertTextFormat = item.InsertTextFormat,
                TextEdit = item.TextEdit,
                AdditionalTextEdits = item.AdditionalTextEdits,
                CommitCharacters = item.CommitCharacters,
                Command = item.Command,
                Data = item.Data,
                Icon = icon
            };
        }

        private class VSLspCompletionItem : CompletionItem
        {
            public PascalCasedSerializableImageElement? Icon { get; set; }
        }

        [DataContract]
        private struct PascalCasedSerializableImageElement
        {
            public PascalCasedSerializableImageElement(PascalCasedSerializableImageId imageId, string automationName)
            {
                ImageId = imageId;
                AutomationName = automationName;
            }

            [DataMember(Name = "ImageId")]
            public PascalCasedSerializableImageId ImageId { get; set; }

            [DataMember(Name = "AutomationName")]
            public string AutomationName { get; set; }
        }

        [DataContract]
        private struct PascalCasedSerializableImageId
        {
            public PascalCasedSerializableImageId(Guid guid, int id)
            {
                Guid = guid;
                Id = id;
            }

            [DataMember(Name = "Guid")]
            public Guid Guid { get; set; }

            [DataMember(Name = "Id")]
            public int Id { get; set; }
        }
    }
}
