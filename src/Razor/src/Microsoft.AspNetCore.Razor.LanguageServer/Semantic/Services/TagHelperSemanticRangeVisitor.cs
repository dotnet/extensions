// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class TagHelperSemanticRangeVisitor : SyntaxWalker
    {
        private readonly List<SemanticRange> _semanticRanges;
        private readonly RazorCodeDocument _razorCodeDocument;
        private readonly Range _range;

        private TagHelperSemanticRangeVisitor(RazorCodeDocument razorCodeDocument, Range range)
        {
            _semanticRanges = new List<SemanticRange>();
            _razorCodeDocument = razorCodeDocument;
            _range = range;
        }

        public static IReadOnlyList<SemanticRange> VisitAllNodes(RazorCodeDocument razorCodeDocument, Range range = null)
        {
            var visitor = new TagHelperSemanticRangeVisitor(razorCodeDocument, range);

            visitor.Visit(razorCodeDocument.GetSyntaxTree().Root);

            return visitor._semanticRanges;
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                var result = CreateSemanticRange(node.Name, SyntaxKind.MarkupTagHelperStartTag);
                AddNode(result);
            }
            base.VisitMarkupTagHelperStartTag(node);
        }

        public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
        {
            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                var result = CreateSemanticRange(node.Name, SyntaxKind.MarkupTagHelperEndTag);
                AddNode(result);
            }
            base.VisitMarkupTagHelperEndTag(node);
        }

        public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                var result = CreateSemanticRange(node.Name, SyntaxKind.MarkupMinimizedTagHelperAttribute);
                AddNode(result);
            }

            base.VisitMarkupMinimizedTagHelperAttribute(node);
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                var result = CreateSemanticRange(node.Name, SyntaxKind.MarkupTagHelperAttribute);
                AddNode(result);
            }

            base.VisitMarkupTagHelperAttribute(node);
        }

        public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                var transition = CreateSemanticRange(node.Transition, SyntaxKind.Transition);
                AddNode(transition);

                var directiveAttribute = CreateSemanticRange(node.Name, SyntaxKind.MarkupTagHelperDirectiveAttribute);
                AddNode(directiveAttribute);

                if (node.Colon != null)
                {
                    var colon = CreateSemanticRange(node.Colon, SyntaxKind.Colon);
                    AddNode(colon);
                }

                if (node.ParameterName != null)
                {
                    var parameterName = CreateSemanticRange(node.ParameterName, SyntaxKind.MarkupTagHelperDirectiveAttribute);
                    AddNode(parameterName);
                }
            }

            base.VisitMarkupTagHelperDirectiveAttribute(node);
        }

        public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                var transition = CreateSemanticRange(node.Transition, SyntaxKind.Transition);
                AddNode(transition);

                var directiveAttribute = CreateSemanticRange(node.Name, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute);
                AddNode(directiveAttribute);

                if (node.Colon != null)
                {
                    var colon = CreateSemanticRange(node.Colon, SyntaxKind.Colon);
                    AddNode(colon);
                }

                if (node.ParameterName != null)
                {
                    var parameterName = CreateSemanticRange(node.ParameterName, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute);

                    AddNode(parameterName);
                }
            }

            base.VisitMarkupMinimizedTagHelperDirectiveAttribute(node);
        }

        private void AddNode(SemanticRange semanticRange)
        {
            if (_range is null || semanticRange.Range.OverlapsWith(_range))
            {
                _semanticRanges.Add(semanticRange);
            }
        }

        // We don't want to classify TagNames of well-known HTML
        // elements as TagHelpers (even if they are). So the 'input' in`<input @onclick='...' />`
        // needs to not be marked as a TagHelper, but `<Input @onclick='...' />` should be.
        private bool ClassifyTagName(MarkupTagHelperElementSyntax node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.StartTag != null && node.StartTag.Name != null)
            {
                var name = node.StartTag.Name.Content;

                if (!HtmlFactsService.IsHtmlTagName(name))
                {
                    // We always classify non-HTML tag names as TagHelpers if they're within a MarkupTagHelperElementSyntax
                    return true;
                }

                // This must be a well-known HTML tag name like 'input', 'br'.

                var binding = node.TagHelperInfo.BindingResult;
                foreach (var descriptor in binding.Descriptors)
                {
                    if (!descriptor.IsComponentTagHelper())
                    {
                        return false;
                    }
                }

                if (name.Length > 0 && char.IsUpper(name[0]))
                {
                    // pascal cased Component TagHelper tag name such as <Input>
                    return true;
                }
            }

            return false;
        }

        private SemanticRange CreateSemanticRange(SyntaxNode node, SyntaxKind kind)
        {
            uint kindUint;
            switch (kind)
            {
                case SyntaxKind.MarkupTagHelperDirectiveAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute:
                    kindUint = SemanticTokensLegend.TokenTypesLegend[SemanticTokensLegend.RazorDirectiveAttribute];
                    break;
                case SyntaxKind.MarkupTagHelperStartTag:
                case SyntaxKind.MarkupTagHelperEndTag:
                    kindUint = SemanticTokensLegend.TokenTypesLegend[SemanticTokensLegend.RazorTagHelperElement];
                    break;
                case SyntaxKind.MarkupTagHelperAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperAttribute:
                    kindUint = SemanticTokensLegend.TokenTypesLegend[SemanticTokensLegend.RazorTagHelperAttribute];
                    break;
                case SyntaxKind.Transition:
                    kindUint = SemanticTokensLegend.TokenTypesLegend[SemanticTokensLegend.RazorTransition];
                    break;
                case SyntaxKind.Colon:
                    kindUint = SemanticTokensLegend.TokenTypesLegend[SemanticTokensLegend.RazorDirectiveColon];
                    break;
                default:
                    throw new NotImplementedException();
            }

            var source = _razorCodeDocument.Source;
            var range = node.GetRange(source);

            var result = new SemanticRange(kindUint, range);

            return result;
        }
    }
}
