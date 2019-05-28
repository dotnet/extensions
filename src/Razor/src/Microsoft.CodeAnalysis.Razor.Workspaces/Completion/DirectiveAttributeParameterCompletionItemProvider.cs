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
    internal class DirectiveAttributeParameterCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
    {
        private readonly TagHelperFactsService _tagHelperFactsService;

        [ImportingConstructor]
        public DirectiveAttributeParameterCompletionItemProvider(TagHelperFactsService tagHelperFactsService)
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
                // Directive attribute parameters are only supported in components
                return Array.Empty<RazorCompletionItem>();
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return Array.Empty<RazorCompletionItem>();
            }

            if (!TryGetAttributeInfo(owner, out var attributeName, out _, out var parameterName, out var parameterNameLocation))
            {
                // Either we're not in an attribute or the attribute is so malformed that we can't provide proper completions.
                return Array.Empty<RazorCompletionItem>();
            }

            if (!parameterNameLocation.IntersectsWith(location.AbsoluteIndex))
            {
                // We're trying to retrieve completions on a portion of the name that is not supported (such as the name, i.e., |@bind|:format).
                return Array.Empty<RazorCompletionItem>();
            }

            if (!TryGetElementInfo(owner.Parent.Parent, out var containingTagName, out var attributes))
            {
                // This should never be the case, it means that we're operating on an attribute that doesn't have a tag.
                return Array.Empty<RazorCompletionItem>();
            }

            var completions = GetAttributeParameterCompletions(attributeName, parameterName, containingTagName, attributes, tagHelperDocumentContext);
            return completions;
        }

        // Internal for testing
        internal IReadOnlyList<RazorCompletionItem> GetAttributeParameterCompletions(
            string attributeName,
            string parameterName,
            string containingTagName,
            IEnumerable<string> attributes,
            TagHelperDocumentContext tagHelperDocumentContext)
        {
            var descriptorsForTag = _tagHelperFactsService.GetTagHelpersGivenTag(tagHelperDocumentContext, containingTagName, parentTag: null);
            if (descriptorsForTag.Count == 0)
            {
                // If the current tag has no possible descriptors then we can't have any additional attributes.
                return Array.Empty<RazorCompletionItem>();
            }

            // Attribute parameters are case sensitive when matching
            var attributeCompletions = new Dictionary<string, HashSet<AttributeDescriptionInfo>>(StringComparer.Ordinal);
            foreach (var descriptor in descriptorsForTag)
            {
                for (var i = 0; i < descriptor.BoundAttributes.Count; i++)
                {
                    var attributeDescriptor = descriptor.BoundAttributes[i];
                    var boundAttributeParameters = attributeDescriptor.BoundAttributeParameters;
                    if (boundAttributeParameters.Count == 0)
                    {
                        continue;
                    }

                    if (TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, attributeDescriptor))
                    {
                        for (var j = boundAttributeParameters.Count - 1; j >= 0; j--)
                        {
                            var parameterDescriptor = boundAttributeParameters[j];

                            if (attributes.Any(name => TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(name, attributeDescriptor, parameterDescriptor)))
                            {
                                // There's already an existing attribute that satisfies this parameter, don't show it in the completion list.
                                continue;
                            }

                            if (!attributeCompletions.TryGetValue(parameterDescriptor.Name, out var attributeDescriptionInfos))
                            {
                                attributeDescriptionInfos = new HashSet<AttributeDescriptionInfo>();
                                attributeCompletions[parameterDescriptor.Name] = attributeDescriptionInfos;
                            }

                            var descriptionInfo = new AttributeDescriptionInfo(
                                parameterDescriptor.TypeName,
                                descriptor.GetTypeName(),
                                parameterDescriptor.GetPropertyName(),
                                parameterDescriptor.Documentation);
                            attributeDescriptionInfos.Add(descriptionInfo);
                        }
                    }
                }
            }

            var completionItems = new List<RazorCompletionItem>();
            foreach (var completion in attributeCompletions)
            {
                if (string.Equals(completion.Key, parameterName, StringComparison.Ordinal))
                {
                    // This completion is identical to the selected parameter, don't provide for completions for what's already
                    // present in the document.
                    continue;
                }

                var razorCompletionItem = new RazorCompletionItem(
                    completion.Key,
                    completion.Key,
                    RazorCompletionItemKind.DirectiveAttributeParameter);
                var completionDescription = new AttributeCompletionDescription(completion.Value.ToArray());
                razorCompletionItem.SetAttributeCompletionDescription(completionDescription);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }
    }
}
