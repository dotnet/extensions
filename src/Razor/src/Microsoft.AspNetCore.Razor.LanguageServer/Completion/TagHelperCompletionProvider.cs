// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class TagHelperCompletionProvider : RazorCompletionItemProvider
    {
        // Internal for testing
        internal static readonly IReadOnlyCollection<string> MinimizedAttributeCommitCharacters = new List<string> { "=", " " };
        internal static readonly IReadOnlyCollection<string> AttributeCommitCharacters = new List<string> { "=" };

        private static readonly IReadOnlyCollection<string> ElementCommitCharacters = new List<string> { " ", ">" };
        private static readonly IReadOnlyCollection<string> NoCommitCharacters = new List<string>();
        private readonly HtmlFactsService _htmlFactsService;
        private readonly TagHelperCompletionService _tagHelperCompletionService;
        private readonly TagHelperFactsService _tagHelperFactsService;

        public TagHelperCompletionProvider(
            TagHelperCompletionService tagHelperCompletionService,
            HtmlFactsService htmlFactsService,
            TagHelperFactsService tagHelperFactsService)
        {
            if (tagHelperCompletionService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperCompletionService));
            }

            if (htmlFactsService is null)
            {
                throw new ArgumentNullException(nameof(htmlFactsService));
            }

            if (tagHelperFactsService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperFactsService));
            }

            _tagHelperCompletionService = tagHelperCompletionService;
            _htmlFactsService = htmlFactsService;
            _tagHelperFactsService = tagHelperFactsService;
        }


        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, TagHelperDocumentContext tagHelperDocumentContext, SourceSpan location)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (tagHelperDocumentContext is null)
            {
                throw new ArgumentNullException(nameof(tagHelperDocumentContext));
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return Array.Empty<RazorCompletionItem>();
            }

            var parent = owner.Parent;
            if (_htmlFactsService.TryGetElementInfo(parent, out var containingTagNameToken, out var attributes) &&
                containingTagNameToken.Span.IntersectsWith(location.AbsoluteIndex))
            {
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);
                var elementCompletions = GetElementCompletions(parent, containingTagNameToken.Content, stringifiedAttributes, tagHelperDocumentContext);
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
                selectedAttributeNameLocation?.IntersectsWith(location.AbsoluteIndex) == true ||
                (prefixLocation?.IntersectsWith(location.AbsoluteIndex) ?? false)))
            {
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);
                var attributeCompletions = GetAttributeCompletions(parent, containingTagNameToken.Content, selectedAttributeName, stringifiedAttributes, tagHelperDocumentContext);
                return attributeCompletions;
            }

            // Invalid location for TagHelper completions.
            return Array.Empty<RazorCompletionItem>();
        }

        private IReadOnlyList<RazorCompletionItem> GetAttributeCompletions(
            SyntaxNode containingAttribute,
            string containingTagName,
            string? selectedAttributeName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            TagHelperDocumentContext tagHelperDocumentContext)
        {
            var ancestors = containingAttribute.Parent.Ancestors();
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

            var completionItems = new List<RazorCompletionItem>();
            var completionResult = _tagHelperCompletionService.GetAttributeCompletions(attributeCompletionContext);
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

                var razorCompletionItem = new RazorCompletionItem(
                    displayText: completion.Key,
                    insertText: filterText,
                    RazorCompletionItemKind.TagHelperAttribute,
                    attributeCommitCharacters);

                var attributeDescriptions = completion.Value.Select(boundAttribute => new TagHelperAttributeDescriptionInfo(
                    boundAttribute.DisplayName,
                    boundAttribute.GetPropertyName(),
                    indexerCompletion ? boundAttribute.IndexerTypeName : boundAttribute.TypeName,
                    boundAttribute.Documentation));
                var attributeDescriptionInfo = new AttributeDescriptionInfo(attributeDescriptions.ToList());
                razorCompletionItem.SetTagHelperAttributeDescriptionInfo(attributeDescriptionInfo);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }

        private IReadOnlyList<RazorCompletionItem> GetElementCompletions(
            SyntaxNode containingTag,
            string containingTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            TagHelperDocumentContext tagHelperDocumentContext)
        {
            var ancestors = containingTag.Ancestors();
            var (ancestorTagName, ancestorIsTagHelper) = _tagHelperFactsService.GetNearestAncestorTagInfo(ancestors);
            var elementCompletionContext = new ElementCompletionContext(
                tagHelperDocumentContext,
                existingCompletions: Enumerable.Empty<string>(),
                containingTagName,
                attributes,
                ancestorTagName,
                ancestorIsTagHelper,
                HtmlFactsService.IsHtmlTagName);

            var completionItems = new List<RazorCompletionItem>();
            var completionResult = _tagHelperCompletionService.GetElementCompletions(elementCompletionContext);
            foreach (var completion in completionResult.Completions)
            {
                var razorCompletionItem = new RazorCompletionItem(
                    displayText: completion.Key,
                    insertText: completion.Key,
                    RazorCompletionItemKind.TagHelperElement,
                    ElementCommitCharacters);

                var tagHelperDescriptions = completion.Value.Select(tagHelper => new TagHelperDescriptionInfo(tagHelper.GetTypeName(), tagHelper.Documentation));
                var elementDescription = new ElementDescriptionInfo(tagHelperDescriptions.ToList());
                razorCompletionItem.SetTagHelperElementDescriptionInfo(elementDescription);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }

        private IReadOnlyCollection<string> ResolveAttributeCommitCharacters(IEnumerable<BoundAttributeDescriptor> boundAttributes, bool indexerCompletion)
        {
            if (indexerCompletion)
            {
                return NoCommitCharacters;
            }
            else if (boundAttributes.Any(b => b.TypeName == "System.Boolean"))
            {
                // Have to use string type because IsBooleanProperty isn't set
                return MinimizedAttributeCommitCharacters;
            }

            return AttributeCommitCharacters;
        }
    }
}
