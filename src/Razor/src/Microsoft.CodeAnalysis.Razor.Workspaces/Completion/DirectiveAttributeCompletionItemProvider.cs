// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    [Shared]
    [Export(typeof(RazorCompletionItemProvider))]
    internal class DirectiveAttributeCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
    {
        private readonly TagHelperFactsService _tagHelperFactsService;

        [ImportingConstructor]
        public DirectiveAttributeCompletionItemProvider(TagHelperFactsService tagHelperFactsService)
        {
            if (tagHelperFactsService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperFactsService));
            }

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

            if (!FileKinds.IsComponent(syntaxTree.Options.FileKind))
            {
                // Directive attributes are only supported in components
                return Array.Empty<RazorCompletionItem>();
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return Array.Empty<RazorCompletionItem>();
            }

            if (!TryGetAttributeInfo(owner, out var attributeName, out var attributeNameLocation, out _, out _))
            {
                // Either we're not in an attribute or the attribute is so malformed that we can't provide proper completions.
                return Array.Empty<RazorCompletionItem>();
            }

            if (!attributeNameLocation.IntersectsWith(location.AbsoluteIndex))
            {
                // We're trying to retrieve completions on a portion of the name that is not supported (such as a parameter).
                return Array.Empty<RazorCompletionItem>();
            }

            if (!TryGetElementInfo(owner.Parent.Parent, out var containingTagName, out var attributes))
            {
                // This should never be the case, it means that we're operating on an attribute that doesn't have a tag.
                return Array.Empty<RazorCompletionItem>();
            }

            // At this point we've determined that completions have been requested for the name portion of the selected attribute.

            var completionItems = GetAttributeCompletions(attributeName, containingTagName, attributes, tagHelperDocumentContext);

            return completionItems;
        }

        // Internal for testing
        internal IReadOnlyList<RazorCompletionItem> GetAttributeCompletions(
            string selectedAttributeName,
            string containingTagName,
            IEnumerable<string> attributes,
            TagHelperDocumentContext tagHelperDocumentContext)
        {
            var descriptorsForTag = _tagHelperFactsService.GetTagHelpersGivenTag(tagHelperDocumentContext, containingTagName, parentTag: null);
            if (descriptorsForTag.Count == 0)
            {
                // If the current tag has no possible descriptors then we can't have any directive attributes.
                return Array.Empty<RazorCompletionItem>();
            }

            // Attributes are case sensitive when matching
            var attributeCompletions = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < descriptorsForTag.Count; i++)
            {
                var descriptor = descriptorsForTag[i];

                foreach (var attributeDescriptor in descriptor.BoundAttributes)
                {
                    if (!attributeDescriptor.IsDirectiveAttribute())
                    {
                        // We don't care about non-directive attributes
                        continue;
                    }

                    if (!TryAddCompletion(attributeDescriptor.Name) && attributeDescriptor.BoundAttributeParameters.Count > 0)
                    {
                        // This attribute has parameters and the base attribute name (@bind) is already satisfied. We need to check if there are any valid
                        // parameters left to be provided, if so, we need to still represent the base attribute name in the completion list.

                        for (var j = 0; j < attributeDescriptor.BoundAttributeParameters.Count; j++)
                        {
                            var parameterDescriptor = attributeDescriptor.BoundAttributeParameters[j];
                            if (!attributes.Any(name => TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(name, attributeDescriptor, parameterDescriptor)))
                            {
                                // This bound attribute parameter has not had a completion entry added for it, re-represent the base attribute name in the completion list
                                attributeCompletions.Add(attributeDescriptor.Name);
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(attributeDescriptor.IndexerNamePrefix))
                    {
                        TryAddCompletion(attributeDescriptor.IndexerNamePrefix + "...");
                    }
                }
            }

            var completionItems = new List<RazorCompletionItem>();
            foreach (var completion in attributeCompletions)
            {
                var insertText = completion;
                if (insertText.EndsWith("..."))
                {
                    // Indexer attribute, we don't want to insert with the triple dot.
                    insertText = insertText.Substring(0, insertText.Length - 3);
                }

                var razorCompletionItem = new RazorCompletionItem(
                    completion,
                    insertText,
                    description: string.Empty,
                    RazorCompletionItemKind.DirectiveAttribute);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;

            bool TryAddCompletion(string attributeName)
            {
                if (attributes.Any(name => string.Equals(name, attributeName, StringComparison.Ordinal)) &&
                    !string.Equals(selectedAttributeName, attributeName, StringComparison.Ordinal))
                {
                    // Attribute is already present on this element and it is not the selected attribute.
                    // It shouldn't exist in the completion list.
                    return false;
                }

                attributeCompletions.Add(attributeName);

                return true;
            }
        }
    }
}