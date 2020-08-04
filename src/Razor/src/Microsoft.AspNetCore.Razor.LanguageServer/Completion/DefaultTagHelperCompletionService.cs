// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.TagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class DefaultTagHelperCompletionService : TagHelperCompletionService
    {
        private static readonly Container<string> NoCommitCharacters = new Container<string>();
        private static readonly Container<string> AttributeCommitCharacters = new Container<string>("=", " ");
        private static readonly Container<string> MinimizedAttributeCommitCharacters = new Container<string>("=");
        private static readonly Container<string> ElementCommitCharacters = new Container<string>(" ", ">");
        private readonly HtmlFactsService _htmlFactsService;
        private readonly RazorTagHelperCompletionService _razorTagHelperCompletionService;
        private readonly TagHelperFactsService _tagHelperFactsService;

        public DefaultTagHelperCompletionService(
            RazorTagHelperCompletionService razorCompletionService,
            HtmlFactsService htmlFactsService,
            TagHelperFactsService tagHelperFactsService)
        {
            if (razorCompletionService is null)
            {
                throw new ArgumentNullException(nameof(razorCompletionService));
            }

            if (htmlFactsService is null)
            {
                throw new ArgumentNullException(nameof(htmlFactsService));
            }

            if (tagHelperFactsService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperFactsService));
            }

            _razorTagHelperCompletionService = razorCompletionService;
            _htmlFactsService = htmlFactsService;
            _tagHelperFactsService = tagHelperFactsService;
        }

        public override IReadOnlyList<CompletionItem> GetCompletionsAt(SourceSpan location, RazorCodeDocument codeDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return Array.Empty<CompletionItem>();
            }

            var parent = owner.Parent;
            if (_htmlFactsService.TryGetElementInfo(parent, out var containingTagNameToken, out var attributes) &&
                containingTagNameToken.Span.IntersectsWith(location.AbsoluteIndex))
            {
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);
                var elementCompletions = GetElementCompletions(parent, containingTagNameToken.Content, stringifiedAttributes, codeDocument);
                return elementCompletions;
            }

            if (_htmlFactsService.TryGetAttributeInfo(
                    parent,
                    out containingTagNameToken,
                    out var prefixLocation,
                    out var selectedAttributeName,
                    out var selectedAttributeNameLocation,
                    out attributes) &&
                (selectedAttributeName == null ||
                selectedAttributeNameLocation.Value.IntersectsWith(location.AbsoluteIndex) ||
                (prefixLocation?.IntersectsWith(location.AbsoluteIndex) ?? false)))
            {
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);
                var attributeCompletions = GetAttributeCompletions(parent, containingTagNameToken.Content, selectedAttributeName, stringifiedAttributes, codeDocument);
                return attributeCompletions;
            }

            // Invalid location for TagHelper completions.
            return Array.Empty<CompletionItem>();
        }

        private IReadOnlyList<CompletionItem> GetAttributeCompletions(
            SyntaxNode containingAttribute,
            string containingTagName,
            string selectedAttributeName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            RazorCodeDocument codeDocument)
        {
            var ancestors = containingAttribute.Parent.Ancestors();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var nonDirectiveAttributeTagHelpers = tagHelperDocumentContext.TagHelpers.Where(tagHelper => !tagHelper.BoundAttributes.Any(attribute => attribute.IsDirectiveAttribute()));
            var filteredContext = TagHelperDocumentContext.Create(tagHelperDocumentContext.Prefix, nonDirectiveAttributeTagHelpers);
            var (ancestorTagName, ancestorIsTagHelper) = _tagHelperFactsService.GetNearestAncestorTagInfo(ancestors);
            var attributeCompletionContext = new AttributeCompletionContext(
                filteredContext,
                existingCompletions: Enumerable.Empty<string>(),
                containingTagName,
                selectedAttributeName,
                attributes,
                ancestorTagName,
                ancestorIsTagHelper,
                HtmlFactsService.IsHtmlTagName);

            var completionItems = new List<CompletionItem>();
            var completionResult = _razorTagHelperCompletionService.GetAttributeCompletions(attributeCompletionContext);
            foreach (var completion in completionResult.Completions)
            {
                var filterText = completion.Key;

                // This is a little bit of a hack because the information returned by _razorTagHelperCompletionService.GetAttributeCompletions
                // does not have enough information for us to determine if a completion is an indexer completion or not. Therefore we have to
                // jump through a few hoops below to:
                //   1. Determine if this specific completion is an indexer based completion
                //   2. Resolve an appropriate snippet if it is. This is more troublesome because we need to remove the ... suffix to accurately
                //      build a snippet that makes sense for the user to type.
                var indexerCompletion = filterText.EndsWith("...", StringComparison.Ordinal);
                if (indexerCompletion)
                {
                    filterText = filterText.Substring(0, filterText.Length - 3);
                }

                var attributeCommitCharacters = ResolveAttributeCommitCharacters(completion.Value, indexerCompletion);

                var razorCompletionItem = new CompletionItem()
                {
                    Label = completion.Key,
                    InsertText = filterText,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    FilterText = filterText,
                    SortText = filterText,
                    Kind = CompletionItemKind.TypeParameter,
                    CommitCharacters = attributeCommitCharacters,
                };

                var attributeDescriptions = completion.Value.Select(boundAttribute => new TagHelperAttributeDescriptionInfo(
                    boundAttribute.DisplayName,
                    boundAttribute.GetPropertyName(),
                    indexerCompletion ? boundAttribute.IndexerTypeName : boundAttribute.TypeName,
                    boundAttribute.Documentation));
                var attributeDescriptionInfo = new AttributeDescriptionInfo(attributeDescriptions.ToList());
                razorCompletionItem.SetDescriptionInfo(attributeDescriptionInfo);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }

        private IReadOnlyList<CompletionItem> GetElementCompletions(
            SyntaxNode containingTag,
            string containingTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            RazorCodeDocument codeDocument)
        {
            var ancestors = containingTag.Ancestors();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var (ancestorTagName, ancestorIsTagHelper) = _tagHelperFactsService.GetNearestAncestorTagInfo(ancestors);
            var elementCompletionContext = new ElementCompletionContext(
                tagHelperDocumentContext,
                existingCompletions: Enumerable.Empty<string>(),
                containingTagName,
                attributes,
                ancestorTagName,
                ancestorIsTagHelper,
                HtmlFactsService.IsHtmlTagName);

            var completionItems = new List<CompletionItem>();
            var completionResult = _razorTagHelperCompletionService.GetElementCompletions(elementCompletionContext);
            foreach (var completion in completionResult.Completions)
            {
                var razorCompletionItem = new CompletionItem()
                {
                    Label = completion.Key,
                    InsertText = completion.Key,
                    FilterText = completion.Key,
                    SortText = completion.Key,
                    Kind = CompletionItemKind.TypeParameter,
                    CommitCharacters = ElementCommitCharacters,
                };

                var tagHelperDescriptions = completion.Value.Select(tagHelper => new TagHelperDescriptionInfo(tagHelper.GetTypeName(), tagHelper.Documentation));
                var elementDescription = new ElementDescriptionInfo(tagHelperDescriptions.ToList());
                razorCompletionItem.SetDescriptionInfo(elementDescription);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }

        private Container<string> ResolveAttributeCommitCharacters(IEnumerable<BoundAttributeDescriptor> boundAttributes, bool indexerCompletion)
        {
            if (indexerCompletion)
            {
                return NoCommitCharacters;
            }
            else if (boundAttributes.Any(b => b.TypeName == "System.Boolean"))
            {
                // Have to use string type because IsBooleanProperty isn't set
                return AttributeCommitCharacters;
            }

            return MinimizedAttributeCommitCharacters;
        }
    }
}
