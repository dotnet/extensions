// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingVisitor : SyntaxWalker
    {
        private const string HtmlTagName = "html";

        private RazorSourceDocument _source;
        private List<FormattingSpan> _spans;
        private FormattingBlockKind _currentBlockKind;
        private SyntaxNode _currentBlock;
        private int _currentHtmlIndentationLevel = 0;
        private int _currentRazorIndentationLevel = 0;
        private bool _isInClassBody = false;

        public FormattingVisitor(RazorSourceDocument source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
            _spans = new List<FormattingSpan>();
            _currentBlockKind = FormattingBlockKind.Markup;
        }

        public IReadOnlyList<FormattingSpan> FormattingSpans => _spans;

        public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Comment, razorCommentSyntax =>
            {
                WriteSpan(razorCommentSyntax.StartCommentTransition, FormattingSpanKind.Transition);
                WriteSpan(razorCommentSyntax.StartCommentStar, FormattingSpanKind.MetaCode);

                var comment = razorCommentSyntax.Comment;
                if (comment.IsMissing)
                {
                    // We need to generate a formatting span at this position. So insert a marker in its place.
                    comment = (SyntaxToken)SyntaxFactory.Token(SyntaxKind.Marker, string.Empty).Green.CreateRed(razorCommentSyntax, razorCommentSyntax.StartCommentStar.EndPosition);
                }

                _currentRazorIndentationLevel++;
                WriteSpan(comment, FormattingSpanKind.Comment);
                _currentRazorIndentationLevel--;

                WriteSpan(razorCommentSyntax.EndCommentStar, FormattingSpanKind.MetaCode);
                WriteSpan(razorCommentSyntax.EndCommentTransition, FormattingSpanKind.Transition);
            });
        }

        public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
        {
            if (node.Parent is CSharpStatementBodySyntax ||
                node.Parent is CSharpExplicitExpressionBodySyntax ||
                node.Parent is CSharpImplicitExpressionBodySyntax ||
                node.Parent is RazorDirectiveBodySyntax ||
                (_currentBlockKind == FormattingBlockKind.Directive &&
                node.Parent?.Parent is RazorDirectiveBodySyntax))
            {
                // If we get here, it means we don't want this code block to be considered significant.
                // Without this, we would have double indentation in places where
                // CSharpCodeBlock is used as a wrapper block in the syntax tree.

                if (!(node.Parent is RazorDirectiveBodySyntax))
                {
                    _currentRazorIndentationLevel++;
                }

                var isInCodeBlockDirective =
                    node.Parent?.Parent?.Parent is RazorDirectiveSyntax directive &&
                    directive.DirectiveDescriptor.Kind == DirectiveKind.CodeBlock;

                if (isInCodeBlockDirective)
                {
                    // This means this is the code portion of an @code or @functions kind of block.
                    _isInClassBody = true;
                }

                base.VisitCSharpCodeBlock(node);

                if (isInCodeBlockDirective)
                {
                    // Finished visiting the code portion. We are no longer in it.
                    _isInClassBody = false;
                }

                if (!(node.Parent is RazorDirectiveBodySyntax))
                {
                    _currentRazorIndentationLevel--;
                }
                return;
            }

            WriteBlock(node, FormattingBlockKind.Statement, base.VisitCSharpCodeBlock);
        }

        public override void VisitCSharpStatement(CSharpStatementSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Statement, base.VisitCSharpStatement);
        }

        public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Expression, base.VisitCSharpExplicitExpression);
        }

        public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Expression, base.VisitCSharpImplicitExpression);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Directive, base.VisitRazorDirective);
        }

        public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Template, base.VisitCSharpTemplateBlock);
        }

        public override void VisitMarkupBlock(MarkupBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupBlock);
        }

        public override void VisitMarkupElement(MarkupElementSyntax node)
        {
            Visit(node.StartTag);

            // Temporary fix to not break the default Html formatting behavior. Remove after https://github.com/dotnet/aspnetcore/issues/25475.
            if (!string.Equals(node.StartTag?.Name?.Content, HtmlTagName, StringComparison.OrdinalIgnoreCase))
            {
                _currentHtmlIndentationLevel++;
            }

            foreach (var child in node.Body)
            {
                Visit(child);
            }

            // Temporary fix to not break the default Html formatting behavior. Remove after https://github.com/dotnet/aspnetcore/issues/25475.
            if (!string.Equals(node.StartTag?.Name?.Content, HtmlTagName, StringComparison.OrdinalIgnoreCase))
            {
                _currentHtmlIndentationLevel--;
            }

            Visit(node.EndTag);
        }

        public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Tag, n =>
            {
                var children = GetRewrittenMarkupStartTagChildren(node);
                foreach (var child in children)
                {
                    Visit(child);
                }
            });
        }

        public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Tag, n =>
            {
                var children = GetRewrittenMarkupEndTagChildren(node);
                foreach (var child in children)
                {
                    Visit(child);
                }
            });
        }

        public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
        {
            Visit(node.StartTag);
            _currentHtmlIndentationLevel++;
            foreach (var child in node.Body)
            {
                Visit(child);
            }
            _currentHtmlIndentationLevel--;
            Visit(node.EndTag);
        }

        public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Tag, n =>
            {
                foreach (var child in n.Children)
                {
                    Visit(child);
                }
            });
        }

        public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Tag, n =>
            {
                foreach (var child in n.Children)
                {
                    Visit(child);
                }
            });
        }

        public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Markup, n =>
            {
                var equalsSyntax = SyntaxFactory.MarkupTextLiteral(new SyntaxList<SyntaxToken>(node.EqualsToken));
                var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
                Visit(mergedAttributePrefix);
                Visit(node.Value);
                Visit(node.ValueSuffix);
            });
        }

        public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
        {
            Visit(node.Value);
        }

        public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
        {
            Visit(node.Transition);
            Visit(node.Colon);
            Visit(node.Value);
        }

        public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
        {
            Visit(node.Transition);
            Visit(node.Colon);
        }

        public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Markup, n =>
            {
                var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name);
                Visit(mergedAttributePrefix);
            });
        }

        public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.HtmlComment, base.VisitMarkupCommentBlock);
        }

        public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupDynamicAttributeValue);
        }

        public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
        {
            WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupTagHelperAttributeValue);
        }

        public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.MetaCode);
            base.VisitRazorMetaCode(node);
        }

        public override void VisitCSharpTransition(CSharpTransitionSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Transition);
            base.VisitCSharpTransition(node);
        }

        public override void VisitMarkupTransition(MarkupTransitionSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Transition);
            base.VisitMarkupTransition(node);
        }

        public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Code);
            base.VisitCSharpStatementLiteral(node);
        }

        public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Code);
            base.VisitCSharpExpressionLiteral(node);
        }

        public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Code);
            base.VisitCSharpEphemeralTextLiteral(node);
        }

        public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.None);
            base.VisitUnclassifiedTextLiteral(node);
        }

        public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Markup);
            base.VisitMarkupLiteralAttributeValue(node);
        }

        public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
        {
            if (node.Parent is MarkupLiteralAttributeValueSyntax)
            {
                base.VisitMarkupTextLiteral(node);
                return;
            }

            WriteSpan(node, FormattingSpanKind.Markup);
            base.VisitMarkupTextLiteral(node);
        }

        public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
        {
            WriteSpan(node, FormattingSpanKind.Markup);
            base.VisitMarkupEphemeralTextLiteral(node);
        }

        private void WriteBlock<TNode>(TNode node, FormattingBlockKind kind, Action<TNode> handler) where TNode : SyntaxNode
        {
            var previousBlock = _currentBlock;
            var previousKind = _currentBlockKind;

            _currentBlock = node;
            _currentBlockKind = kind;

            handler(node);

            _currentBlock = previousBlock;
            _currentBlockKind = previousKind;
        }

        private void WriteSpan(SyntaxNode node, FormattingSpanKind kind)
        {
            if (node.IsMissing)
            {
                return;
            }

            var spanSource = new TextSpan(node.Position, node.FullWidth);
            var blockSource = new TextSpan(_currentBlock.Position, _currentBlock.FullWidth);

            var span = new FormattingSpan(spanSource, blockSource, kind, _currentBlockKind, _currentRazorIndentationLevel, _currentHtmlIndentationLevel, _isInClassBody);
            _spans.Add(span);
        }

        private static SyntaxList<RazorSyntaxNode> GetRewrittenMarkupStartTagChildren(MarkupStartTagSyntax node)
        {
            // Rewrites the children of the start tag to look like the legacy syntax tree.
            if (node.IsMarkupTransition)
            {
                var tokens = node.DescendantNodes().Where(n => n is SyntaxToken token && !token.IsMissing).Cast<SyntaxToken>().ToArray();
                var tokenBuilder = SyntaxListBuilder<SyntaxToken>.Create();
                tokenBuilder.AddRange(tokens, 0, tokens.Length);
                var markupTransition = SyntaxFactory.MarkupTransition(tokenBuilder.ToList()).Green.CreateRed(node, node.Position);
                var spanContext = node.GetSpanContext();
                if (spanContext != null)
                {
                    markupTransition = markupTransition.WithSpanContext(spanContext);
                }

                var builder = new SyntaxListBuilder(1);
                builder.Add(markupTransition);
                return new SyntaxList<RazorSyntaxNode>(builder.ToListNode().CreateRed(node, node.Position));
            }

            SpanContext latestSpanContext = null;
            var children = node.Children;
            var newChildren = new SyntaxListBuilder(children.Count);
            var literals = new List<MarkupTextLiteralSyntax>();
            foreach (var child in children)
            {
                if (child is MarkupTextLiteralSyntax literal)
                {
                    literals.Add(literal);
                    latestSpanContext = literal.GetSpanContext() ?? latestSpanContext;
                }
                else if (child is MarkupMiscAttributeContentSyntax miscContent)
                {
                    foreach (var contentChild in miscContent.Children)
                    {
                        if (contentChild is MarkupTextLiteralSyntax contentLiteral)
                        {
                            literals.Add(contentLiteral);
                            latestSpanContext = contentLiteral.GetSpanContext() ?? latestSpanContext;
                        }
                        else
                        {
                            // Pop stack
                            AddLiteralIfExists();
                            newChildren.Add(contentChild);
                        }
                    }
                }
                else
                {
                    AddLiteralIfExists();
                    newChildren.Add(child);
                }
            }

            AddLiteralIfExists();

            return new SyntaxList<RazorSyntaxNode>(newChildren.ToListNode().CreateRed(node, node.Position));

            void AddLiteralIfExists()
            {
                if (literals.Count > 0)
                {
                    var mergedLiteral = SyntaxUtilities.MergeTextLiterals(literals.ToArray());
                    mergedLiteral = mergedLiteral.WithSpanContext(latestSpanContext);
                    literals.Clear();
                    latestSpanContext = null;
                    newChildren.Add(mergedLiteral);
                }
            }
        }

        private static SyntaxList<RazorSyntaxNode> GetRewrittenMarkupEndTagChildren(MarkupEndTagSyntax node)
        {
            // Rewrites the children of the end tag to look like the legacy syntax tree.
            if (node.IsMarkupTransition)
            {
                var tokens = node.DescendantNodes().Where(n => n is SyntaxToken token && !token.IsMissing).Cast<SyntaxToken>().ToArray();
                var tokenBuilder = SyntaxListBuilder<SyntaxToken>.Create();
                tokenBuilder.AddRange(tokens, 0, tokens.Length);
                var markupTransition = SyntaxFactory.MarkupTransition(tokenBuilder.ToList()).Green.CreateRed(node, node.Position);
                var spanContext = node.GetSpanContext();
                if (spanContext != null)
                {
                    markupTransition = markupTransition.WithSpanContext(spanContext);
                }

                var builder = new SyntaxListBuilder(1);
                builder.Add(markupTransition);
                return new SyntaxList<RazorSyntaxNode>(builder.ToListNode().CreateRed(node, node.Position));
            }

            return node.Children;
        }
    }
}
