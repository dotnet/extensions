// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        #region HTML
        public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
        {
            AddSemanticRange(node.Name, SyntaxKind.MarkupAttributeBlock);
            AddSemanticRange(node.EqualsToken);
            base.VisitMarkupAttributeBlock(node);
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle);
            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang);
            }
            AddSemanticRange(node.Name, SyntaxKind.MarkupElement);
            base.VisitMarkupStartTag(node);
            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash);
            }
            AddSemanticRange(node.CloseAngle);
        }

        public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle);
            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang);
            }
            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash);
            }
            AddSemanticRange(node.Name, SyntaxKind.MarkupElement);
            AddSemanticRange(node.CloseAngle);
            base.VisitMarkupEndTag(node);
        }

        public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
        {
            Debug.Assert(node.Children.Count == 3, $"There should be 3 nodes but were {node.Children.Count}");
            AddSemanticRange(node.Children[0], RazorSemanticTokensLegend.MarkupCommentPunctuation);
            AddSemanticRange(node.Children[1], SyntaxKind.MarkupCommentBlock);
            AddSemanticRange(node.Children[2], RazorSemanticTokensLegend.MarkupCommentPunctuation);
            base.VisitMarkupCommentBlock(node);
        }

        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            AddSemanticRange(node.Name, SyntaxKind.MarkupAttributeBlock);
            base.VisitMarkupMinimizedAttributeBlock(node);
        }
        #endregion HTML

        #region Razor
        public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
        {
            AddSemanticRange(node.StartCommentTransition);
            AddSemanticRange(node.StartCommentStar);
            AddSemanticRange(node.Comment);
            AddSemanticRange(node.EndCommentStar);
            AddSemanticRange(node.EndCommentTransition);

            base.VisitRazorCommentBlock(node);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            AddSemanticRange(node.Transition, SyntaxKind.Transition);
            base.VisitRazorDirective(node);
        }

        public override void VisitRazorDirectiveBody(RazorDirectiveBodySyntax node)
        {
            // We can't provide colors for CSharp because if we both provided them then they would overlap, which violates the LSP spec.
            if (node.Keyword.Kind != SyntaxKind.CSharpStatementLiteral)
            {
                AddSemanticRange(node.Keyword, SyntaxKind.RazorDirective);
            }
            base.VisitRazorDirectiveBody(node);
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle);
            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupTagHelperStartTag);
            }
            else
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupElement);
            }

            base.VisitMarkupTagHelperStartTag(node);

            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash);
            }
            AddSemanticRange(node.CloseAngle);
        }

        public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle);
            AddSemanticRange(node.ForwardSlash);
            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupTagHelperEndTag);
            }
            else
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupElement);
            }

            base.VisitMarkupTagHelperEndTag(node);
            AddSemanticRange(node.CloseAngle);
        }

        public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupMinimizedTagHelperAttribute);
            }

            base.VisitMarkupMinimizedTagHelperAttribute(node);
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupTagHelperAttribute);
            }
            else
            {
                AddSemanticRange(node.Name, SyntaxKind.MarkupAttributeBlock);
            }
            AddSemanticRange(node.EqualsToken);

            base.VisitMarkupTagHelperAttribute(node);
        }

        public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Transition, SyntaxKind.Transition);
                AddSemanticRange(node.Name, SyntaxKind.MarkupTagHelperDirectiveAttribute);

                if (node.Colon != null)
                {
                    AddSemanticRange(node.Colon, SyntaxKind.Colon);
                }

                if (node.ParameterName != null)
                {
                    AddSemanticRange(node.ParameterName, SyntaxKind.MarkupTagHelperDirectiveAttribute);
                }
            }

            AddSemanticRange(node.EqualsToken);

            base.VisitMarkupTagHelperDirectiveAttribute(node);
        }

        public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Transition, SyntaxKind.Transition);
                AddSemanticRange(node.Name, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute);

                if (node.Colon != null)
                {
                    AddSemanticRange(node.Colon, SyntaxKind.Colon);
                }

                if (node.ParameterName != null)
                {
                    AddSemanticRange(node.ParameterName, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute);
                }
            }

            base.VisitMarkupMinimizedTagHelperDirectiveAttribute(node);
        }
        #endregion Razor

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

        private void AddSemanticRange(SyntaxNode node, int semanticKind)
        {
            var source = _razorCodeDocument.Source;
            var range = node.GetRange(source);

            var semanticRange = new SemanticRange(semanticKind, range, modifier: 0);

            if (_range is null || semanticRange.Range.OverlapsWith(_range))
            {
                _semanticRanges.Add(semanticRange);
            }
        }

        private void AddSemanticRange(SyntaxNode node, SyntaxKind? kind = null)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (kind is null)
            {
                kind = node.Kind;
            }

            if (node.Width == 0)
            {
                // Under no circumstances can we have 0-width spans.
                // This can happen in situations like "@* comment ", where EndCommentStar and EndCommentTransition are empty.
                return;
            }

            int semanticKind;
            switch (kind)
            {
                case SyntaxKind.MarkupTagHelperDirectiveAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute:
                    semanticKind = RazorSemanticTokensLegend.RazorDirectiveAttribute;
                    break;
                case SyntaxKind.MarkupTagHelperStartTag:
                case SyntaxKind.MarkupTagHelperEndTag:
                    semanticKind = RazorSemanticTokensLegend.RazorTagHelperElement;
                    break;
                case SyntaxKind.MarkupTagHelperAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperAttribute:
                    semanticKind = RazorSemanticTokensLegend.RazorTagHelperAttribute;
                    break;
                case SyntaxKind.Transition:
                    semanticKind = RazorSemanticTokensLegend.RazorTransition;
                    break;
                case SyntaxKind.Colon:
                    semanticKind = RazorSemanticTokensLegend.RazorDirectiveColon;
                    break;
                case SyntaxKind.RazorDirective:
                    semanticKind = RazorSemanticTokensLegend.RazorDirective;
                    break;
                case SyntaxKind.RazorCommentTransition:
                    semanticKind = RazorSemanticTokensLegend.RazorCommentTransition;
                    break;
                case SyntaxKind.RazorCommentStar:
                    semanticKind = RazorSemanticTokensLegend.RazorCommentStar;
                    break;
                case SyntaxKind.RazorCommentLiteral:
                    semanticKind = RazorSemanticTokensLegend.RazorComment;
                    break;
                case SyntaxKind.RazorComment:
                    semanticKind = RazorSemanticTokensLegend.RazorComment;
                    break;
                case SyntaxKind.OpenAngle:
                case SyntaxKind.CloseAngle:
                case SyntaxKind.ForwardSlash:
                    semanticKind = RazorSemanticTokensLegend.MarkupTagDelimiter;
                    break;
                case SyntaxKind.Equals:
                    semanticKind = RazorSemanticTokensLegend.MarkupOperator;
                    break;
                case SyntaxKind.MarkupElement:
                    semanticKind = RazorSemanticTokensLegend.MarkupElement;
                    break;
                case SyntaxKind.MarkupAttributeBlock:
                    semanticKind = RazorSemanticTokensLegend.MarkupAttribute;
                    break;
                case SyntaxKind.MarkupCommentBlock:
                    semanticKind = RazorSemanticTokensLegend.MarkupComment;
                    break;
                default:
                    throw new NotImplementedException();
            }

            AddSemanticRange(node, semanticKind);
        }
    }
}
