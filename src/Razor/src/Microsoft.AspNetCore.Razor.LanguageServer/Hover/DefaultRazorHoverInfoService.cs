// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using HoverModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Hover;
using RangeModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hover
{
    internal class DefaultRazorHoverInfoService : RazorHoverInfoService
    {
        private readonly TagHelperFactsService _tagHelperFactsService;
        private readonly TagHelperDescriptionFactory _tagHelperDescriptionFactory;
        private readonly HtmlFactsService _htmlFactsService;

        [ImportingConstructor]
        public DefaultRazorHoverInfoService(
            TagHelperFactsService tagHelperFactsService,
            TagHelperDescriptionFactory tagHelperDescriptionFactory,
            HtmlFactsService htmlFactsService)
        {
            if (tagHelperFactsService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperFactsService));
            }

            if (tagHelperDescriptionFactory is null)
            {
                throw new ArgumentNullException(nameof(tagHelperDescriptionFactory));
            }

            if (htmlFactsService is null)
            {
                throw new ArgumentNullException(nameof(htmlFactsService));
            }

            _tagHelperFactsService = tagHelperFactsService;
            _tagHelperDescriptionFactory = tagHelperDescriptionFactory;
            _htmlFactsService = htmlFactsService;
        }

        public override HoverModel GetHoverInfo(RazorCodeDocument codeDocument, SourceLocation location)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var syntaxTree = codeDocument.GetSyntaxTree();

            var change = new SourceChange(location.AbsoluteIndex, length: 0, newText: "");
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return null;
            }

            var parent = owner.Parent;
            var position = new Position(location.LineIndex, location.CharacterIndex);
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();

            var ancestors = owner.Ancestors();
            var (parentTag, parentIsTagHelper) = _tagHelperFactsService.GetNearestAncestorTagInfo(ancestors);

            if (_htmlFactsService.TryGetElementInfo(parent, out var containingTagNameToken, out var attributes) &&
                containingTagNameToken.Span.IntersectsWith(location.AbsoluteIndex))
            {
                // Hovering over HTML tag name
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);
                var binding = _tagHelperFactsService.GetTagHelperBinding(
                    tagHelperDocumentContext,
                    containingTagNameToken.Content,
                    stringifiedAttributes,
                    parentTag: parentTag,
                    parentIsTagHelper: parentIsTagHelper);

                if (binding is null)
                {
                    // No matching tagHelpers, it's just HTML
                    return null;
                }
                else
                {
                    Debug.Assert(binding.Descriptors.Any());

                    var range = containingTagNameToken.GetRange(codeDocument.Source);

                    var result = ElementInfoToHover(binding.Descriptors, range);
                    return result;
                }
            }

            if (_htmlFactsService.TryGetAttributeInfo(parent, out containingTagNameToken, out _, out var selectedAttributeName, out var selectedAttributeNameLocation, out attributes) &&
                selectedAttributeNameLocation?.IntersectsWith(location.AbsoluteIndex) == true)
            {
                // Hovering over HTML attribute name
                var stringifiedAttributes = _tagHelperFactsService.StringifyAttributes(attributes);

                var binding = _tagHelperFactsService.GetTagHelperBinding(
                    tagHelperDocumentContext,
                    containingTagNameToken.Content,
                    stringifiedAttributes,
                    parentTag: parentTag,
                    parentIsTagHelper: parentIsTagHelper);

                if (binding is null)
                {
                    // No matching TagHelpers, it's just HTML
                    return null;
                }
                else
                {
                    Debug.Assert(binding.Descriptors.Any());
                    var tagHelperAttributes = _tagHelperFactsService.GetBoundTagHelperAttributes(tagHelperDocumentContext, selectedAttributeName, binding);

                    var attribute = attributes.Single(a => a.Span.IntersectsWith(location.AbsoluteIndex));
                    if (attribute is MarkupTagHelperAttributeSyntax thAttributeSyntax)
                    {
                        attribute = thAttributeSyntax.Name;
                    }
                    else if (attribute is MarkupMinimizedTagHelperAttributeSyntax thMinimizedAttribute)
                    {
                        attribute = thMinimizedAttribute.Name;
                    }
                    else if (attribute is MarkupTagHelperDirectiveAttributeSyntax directiveAttribute)
                    {
                        attribute = directiveAttribute.Name;
                    }
                    else if (attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax miniDirectiveAttribute)
                    {
                        attribute = miniDirectiveAttribute;
                    }

                    var range = attribute.GetRange(codeDocument.Source);

                    // Include the @ in the range
                    switch (attribute.Parent.Kind)
                    {
                        case SyntaxKind.MarkupTagHelperDirectiveAttribute:
                            var directiveAttribute = attribute.Parent as MarkupTagHelperDirectiveAttributeSyntax;
                            range.Start.Character -= directiveAttribute.Transition.FullWidth;
                            break;
                        case SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute:
                            var minimizedAttribute = containingTagNameToken.Parent as MarkupMinimizedTagHelperDirectiveAttributeSyntax;
                            range.Start.Character -= minimizedAttribute.Transition.FullWidth;
                            break;
                    }

                    var attributeHoverModel = AttributeInfoToHover(tagHelperAttributes, range);

                    return attributeHoverModel;
                }
            }

            return null;
        }

        private HoverModel AttributeInfoToHover(IEnumerable<BoundAttributeDescriptor> descriptors, RangeModel range)
        {
            var descriptionInfos = descriptors.Select(d => new TagHelperAttributeDescriptionInfo(d.DisplayName, d.GetPropertyName(), d.TypeName, d.Documentation))
                .ToList()
                .AsReadOnly();
            var attrDescriptionInfo = new AttributeDescriptionInfo(descriptionInfos);

            if (!_tagHelperDescriptionFactory.TryCreateDescription(attrDescriptionInfo, out var markupContent))
            {
                return null;
            }

            var hover = new HoverModel
            {
                Contents = new MarkedStringsOrMarkupContent(markupContent),
                Range = range
            };

            return hover;
        }

        private HoverModel ElementInfoToHover(IEnumerable<TagHelperDescriptor> descriptors, RangeModel range)
        {
            var descriptionInfos = descriptors.Select(d => new TagHelperDescriptionInfo(d.DisplayName, d.Documentation))
                .ToList()
                .AsReadOnly();
            var elementDescriptionInfo = new ElementDescriptionInfo(descriptionInfos);

            if (!_tagHelperDescriptionFactory.TryCreateDescription(elementDescriptionInfo, out var markupContent))
            {
                return null;
            }

            var hover = new HoverModel
            {
                Contents = new MarkedStringsOrMarkupContent(markupContent),
                Range = range
            };

            return hover;
        }
    }
}
