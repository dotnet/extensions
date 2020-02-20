// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [Export(typeof(TagHelperFactsService))]
    internal class DefaultTagHelperFactsService : TagHelperFactsService
    {
        public override TagHelperBinding GetTagHelperBinding(
            TagHelperDocumentContext documentContext,
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTag,
            bool parentIsTagHelper)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var descriptors = documentContext.TagHelpers;
            if (descriptors == null || descriptors.Count == 0)
            {
                return null;
            }

            var prefix = documentContext.Prefix;
            var tagHelperBinder = new TagHelperBinder(prefix, descriptors);
            var binding = tagHelperBinder.GetBinding(tagName, attributes.ToList(), parentTag, parentIsTagHelper);

            return binding;
        }

        public override IEnumerable<BoundAttributeDescriptor> GetBoundTagHelperAttributes(
            TagHelperDocumentContext documentContext,
            string attributeName,
            TagHelperBinding binding)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var matchingBoundAttributes = new List<BoundAttributeDescriptor>();
            foreach (var descriptor in binding.Descriptors)
            {
                foreach (var boundAttributeDescriptor in descriptor.BoundAttributes)
                {
                    if (TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, boundAttributeDescriptor))
                    {
                        matchingBoundAttributes.Add(boundAttributeDescriptor);

                        // Only one bound attribute can match an attribute
                        break;
                    }
                }
            }

            return matchingBoundAttributes;
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenTag(
            TagHelperDocumentContext documentContext,
            string tagName,
            string parentTag)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            var matchingDescriptors = new List<TagHelperDescriptor>();
            var descriptors = documentContext?.TagHelpers;
            if (descriptors?.Count == 0)
            {
                return matchingDescriptors;
            }

            var prefix = documentContext.Prefix ?? string.Empty;
            if (!tagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // Can't possibly match TagHelpers, it doesn't start with the TagHelperPrefix.
                return matchingDescriptors;
            }

            var tagNameWithoutPrefix = tagName.Substring(prefix.Length);
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesTagName(tagNameWithoutPrefix, rule) &&
                        TagHelperMatchingConventions.SatisfiesParentTag(parentTag, rule))
                    {
                        matchingDescriptors.Add(descriptor);
                        break;
                    }
                }
            }

            return matchingDescriptors;
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string parentTag)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            var matchingDescriptors = new List<TagHelperDescriptor>();
            var descriptors = documentContext?.TagHelpers;
            if (descriptors?.Count == 0)
            {
                return matchingDescriptors;
            }

            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesParentTag(parentTag, rule))
                    {
                        matchingDescriptors.Add(descriptor);
                        break;
                    }
                }
            }

            return matchingDescriptors;
        }

        internal override IEnumerable<KeyValuePair<string, string>> StringifyAttributes(SyntaxList<RazorSyntaxNode> attributes)
        {
            var stringifiedAttributes = new List<KeyValuePair<string, string>>();

            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                if (attribute is MarkupTagHelperAttributeSyntax tagHelperAttribute)
                {
                    var name = tagHelperAttribute.Name.GetContent();
                    var value = tagHelperAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute)
                {
                    var name = minimizedTagHelperAttribute.Name.GetContent();
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, string.Empty));
                }
                else if (attribute is MarkupAttributeBlockSyntax markupAttribute)
                {
                    var name = markupAttribute.Name.GetContent();
                    var value = markupAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute)
                {
                    var name = minimizedMarkupAttribute.Name.GetContent();
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, string.Empty));
                }
                else if (attribute is MarkupTagHelperDirectiveAttributeSyntax directiveAttribute)
                {
                    var name = directiveAttribute.FullName;
                    var value = directiveAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute)
                {
                    var name = minimizedDirectiveAttribute.FullName;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, string.Empty));
                }
            }

            return stringifiedAttributes;
        }

        internal override (string ancestorTagName, bool ancestorIsTagHelper) GetNearestAncestorTagInfo(IEnumerable<SyntaxNode> ancestors)
        {
            foreach (var ancestor in ancestors)
            {
                if (ancestor is MarkupElementSyntax element)
                {
                    // It's possible for start tag to be null in malformed cases.
                    var name = element.StartTag?.Name?.Content ?? string.Empty;
                    return (name, ancestorIsTagHelper: false);
                }
                else if (ancestor is MarkupTagHelperElementSyntax tagHelperElement)
                {
                    // It's possible for start tag to be null in malformed cases.
                    var name = tagHelperElement.StartTag?.Name?.Content ?? string.Empty;
                    return (name, ancestorIsTagHelper: true);
                }
            }

            return (ancestorTagName: null, ancestorIsTagHelper: false);
        }
    }
}
