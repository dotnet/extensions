// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable

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
        private readonly Range? _range;

        private TagHelperSemanticRangeVisitor(RazorCodeDocument razorCodeDocument, Range? range)
        {
            _semanticRanges = new List<SemanticRange>();
            _razorCodeDocument = razorCodeDocument;
            _range = range;
        }

        public static IReadOnlyList<SemanticRange> VisitAllNodes(RazorCodeDocument razorCodeDocument, Range? range = null)
        {
            var visitor = new TagHelperSemanticRangeVisitor(razorCodeDocument, range);

            visitor.Visit(razorCodeDocument.GetSyntaxTree().Root);

            return visitor._semanticRanges;
        }

        private void Visit(SyntaxList<RazorSyntaxNode> syntaxNodes)
        {
            for (var i = 0; i < syntaxNodes.Count; i++)
            {
                Visit(syntaxNodes[i]);
            }
        }

        #region HTML
        public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
        {
            Visit(node.NamePrefix);
            AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupAttribute);
            Visit(node.NameSuffix);
            AddSemanticRange(node.EqualsToken, RazorSemanticTokensLegend.MarkupOperator);

            Visit(node.ValuePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang, RazorSemanticTokensLegend.MarkupElement);
            }

            AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupElement);

            Visit(node.Attributes);
            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash, RazorSemanticTokensLegend.MarkupTagDelimiter);
            }
            AddSemanticRange(node.CloseAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
        }

        public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang, RazorSemanticTokensLegend.MarkupElement);
            }

            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash, RazorSemanticTokensLegend.MarkupTagDelimiter);
            }
            AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupElement);
            AddSemanticRange(node.CloseAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
        }

        public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
        {
            AddSemanticRange(node.Children[0], RazorSemanticTokensLegend.MarkupCommentPunctuation);

            for (var i = 1; i < node.Children.Count - 1; i++)
            {
                var commentNode = node.Children[i];
                switch (commentNode.Kind)
                {
                    case SyntaxKind.MarkupTextLiteral:
                        AddSemanticRange(commentNode, RazorSemanticTokensLegend.MarkupComment);
                        break;
                    default:
                        Visit(commentNode);
                        break;
                }
            }

            AddSemanticRange(node.Children[node.Children.Count - 1], RazorSemanticTokensLegend.MarkupCommentPunctuation);
        }

        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            Visit(node.NamePrefix);
            AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupAttribute);
        }
        #endregion HTML

        #region C#
        public override void VisitCSharpStatement(CSharpStatementSyntax node)
        {
            AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
            Visit(node.Body);
        }

        public override void VisitCSharpStatementBody(CSharpStatementBodySyntax node)
        {
            AddSemanticRange(node.OpenBrace, RazorSemanticTokensLegend.RazorTransition);
            Visit(node.CSharpCode);
            AddSemanticRange(node.CloseBrace, RazorSemanticTokensLegend.RazorTransition);
        }

        public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
        {
            AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
            Visit(node.Body);
        }

        public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
        {
            AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
            Visit(node.Body);
        }

        public override void VisitCSharpExplicitExpressionBody(CSharpExplicitExpressionBodySyntax node)
        {
            AddSemanticRange(node.OpenParen, RazorSemanticTokensLegend.CSharpPunctuation);
            Visit(node.CSharpCode);
            AddSemanticRange(node.CloseParen, RazorSemanticTokensLegend.CSharpPunctuation);
        }
        #endregion C#

        #region Razor
        public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
        {
            AddSemanticRange(node.StartCommentTransition, RazorSemanticTokensLegend.RazorCommentTransition);
            AddSemanticRange(node.StartCommentStar, RazorSemanticTokensLegend.RazorCommentStar);
            AddSemanticRange(node.Comment, RazorSemanticTokensLegend.RazorComment);
            AddSemanticRange(node.EndCommentStar, RazorSemanticTokensLegend.RazorCommentStar);
            AddSemanticRange(node.EndCommentTransition, RazorSemanticTokensLegend.RazorCommentTransition);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
            Visit(node.Body);
        }

        public override void VisitRazorDirectiveBody(RazorDirectiveBodySyntax node)
        {
            // We can't provide colors for CSharp because if we both provided them then they would overlap, which violates the LSP spec.
            if (node.Keyword.Kind != SyntaxKind.CSharpStatementLiteral)
            {
                AddSemanticRange(node.Keyword, RazorSemanticTokensLegend.RazorDirective);
            }
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang, RazorSemanticTokensLegend.MarkupElement);
            }

            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorTagHelperElement);
            }
            else
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupElement);
            }

            Visit(node.Attributes);

            if (node.ForwardSlash != null)
            {
                AddSemanticRange(node.ForwardSlash, RazorSemanticTokensLegend.MarkupTagDelimiter);
            }
            AddSemanticRange(node.CloseAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
        }

        public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
        {
            AddSemanticRange(node.OpenAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
            AddSemanticRange(node.ForwardSlash, RazorSemanticTokensLegend.MarkupTagDelimiter);

            if (node.Bang != null)
            {
                AddSemanticRange(node.Bang, RazorSemanticTokensLegend.MarkupElement);
            }

            if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorTagHelperElement);
            }
            else
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupElement);
            }

            AddSemanticRange(node.CloseAngle, RazorSemanticTokensLegend.MarkupTagDelimiter);
        }

        public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
        {
            Visit(node.NamePrefix);

            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorTagHelperAttribute);
            }
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            Visit(node.NamePrefix);
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorTagHelperAttribute);
            }
            else
            {
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.MarkupAttribute);
            }
            Visit(node.NameSuffix);

            AddSemanticRange(node.EqualsToken, RazorSemanticTokensLegend.MarkupOperator);

            Visit(node.ValuePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }

        public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
                Visit(node.NamePrefix);
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorDirectiveAttribute);
                Visit(node.NameSuffix);

                if (node.Colon != null)
                {
                    AddSemanticRange(node.Colon, RazorSemanticTokensLegend.RazorDirectiveColon);
                }

                if (node.ParameterName != null)
                {
                    AddSemanticRange(node.ParameterName, RazorSemanticTokensLegend.RazorDirectiveAttribute);
                }
            }

            AddSemanticRange(node.EqualsToken, RazorSemanticTokensLegend.MarkupOperator);
            Visit(node.ValuePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        }

        public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
        {
            if (node.TagHelperAttributeInfo.Bound)
            {
                AddSemanticRange(node.Transition, RazorSemanticTokensLegend.RazorTransition);
                Visit(node.NamePrefix);
                AddSemanticRange(node.Name, RazorSemanticTokensLegend.RazorDirectiveAttribute);

                if (node.Colon != null)
                {
                    AddSemanticRange(node.Colon, RazorSemanticTokensLegend.RazorDirectiveColon);
                }

                if (node.ParameterName != null)
                {
                    AddSemanticRange(node.ParameterName, RazorSemanticTokensLegend.RazorDirectiveAttribute);
                }
            }
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
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Width == 0)
            {
                // Under no circumstances can we have 0-width spans.
                // This can happen in situations like "@* comment ", where EndCommentStar and EndCommentTransition are empty.
                return;
            }

            var source = _razorCodeDocument.Source;
            var range = node.GetRange(source);

            var semanticRange = new SemanticRange(semanticKind, range, modifier: 0);

            if (_range is null || semanticRange.Range.OverlapsWith(_range))
            {
                _semanticRanges.Add(semanticRange);
            }
        }
    }
}
