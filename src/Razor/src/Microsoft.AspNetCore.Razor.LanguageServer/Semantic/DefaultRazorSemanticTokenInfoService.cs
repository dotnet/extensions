// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class DefaultRazorSemanticTokenInfoService : RazorSemanticTokenInfoService
    {
        public DefaultRazorSemanticTokenInfoService()
        {
        }

        public override SemanticTokens GetSemanticTokens(RazorCodeDocument codeDocument, SourceLocation? location = null)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }
            var syntaxTree = codeDocument.GetSyntaxTree();

            var syntaxNodes = VisitAllNodes(syntaxTree);

            var semanticTokens = ConvertSyntaxTokensToSemanticTokens(syntaxNodes, codeDocument);

            return semanticTokens;
        }

        private static IEnumerable<SyntaxNode> VisitAllNodes(RazorSyntaxTree syntaxTree)
        {
            var visitor = new TagHelperSpanVisitor();
            visitor.Visit(syntaxTree.Root);

            return visitor.TagHelperNodes;
        }

        private static SemanticTokens ConvertSyntaxTokensToSemanticTokens(
            IEnumerable<SyntaxNode> syntaxTokens,
            RazorCodeDocument razorCodeDocument)
        {
            SyntaxNode previousToken = null;

            var data = new List<uint>();
            foreach (var token in syntaxTokens)
            {
                var newData = GetData(token, previousToken, razorCodeDocument);
                data.AddRange(newData);

                previousToken = token;
            }

            return new SemanticTokens
            {
                Data = data
            };
        }

        /**
         * In short, each token takes 5 integers to represent, so a specific token `i` in the file consists of the following array indices:
         *  - at index `5*i`   - `deltaLine`: token line number, relative to the previous token
         *  - at index `5*i+1` - `deltaStart`: token start character, relative to the previous token (relative to 0 or the previous token's start if they are on the same line)
         *  - at index `5*i+2` - `length`: the length of the token. A token cannot be multiline.
         *  - at index `5*i+3` - `tokenType`: will be looked up in `SemanticTokensLegend.tokenTypes`
         *  - at index `5*i+4` - `tokenModifiers`: each set bit will be looked up in `SemanticTokensLegend.tokenModifiers`
        **/
        private static IEnumerable<uint> GetData(
            SyntaxNode currentNode,
            SyntaxNode previousNode,
            RazorCodeDocument razorCodeDocument)
        {
            var previousRange = previousNode?.GetRange(razorCodeDocument.Source);
            var currentRange = currentNode.GetRange(razorCodeDocument.Source);

            // deltaLine
            var previousLineIndex = previousNode == null ? 0 : previousRange.Start.Line;
            yield return (uint)(currentRange.Start.Line - previousLineIndex);

            // deltaStart
            if (previousRange != null && previousRange?.Start.Line == currentRange.Start.Line)
            {
                yield return (uint)(currentRange.Start.Character - previousRange.Start.Character);
            }
            else
            {
                yield return (uint)(currentRange.Start.Character);
            }

            // length
            Debug.Assert(currentNode.Span.Length > 0);
            yield return (uint)currentNode.Span.Length;

            // tokenType
            yield return GetTokenTypeData(currentNode);

            // tokenModifiers
            // We don't currently have any need for tokenModifiers
            yield return 0;
        }

        private static uint GetTokenTypeData(SyntaxNode syntaxToken)
        {
            switch (syntaxToken.Parent.Kind)
            {
                case SyntaxKind.MarkupTagHelperStartTag:
                case SyntaxKind.MarkupTagHelperEndTag:
                    return (uint)SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorTagHelperElement];
                case SyntaxKind.MarkupTagHelperAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute:
                case SyntaxKind.MarkupTagHelperDirectiveAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperAttribute:
                    return (uint)SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorTagHelperAttribute];
                default:
                    throw new NotImplementedException();
            }
        }

        private class TagHelperSpanVisitor : SyntaxWalker
        {
            private readonly List<SyntaxNode> _syntaxNodes;

            public TagHelperSpanVisitor()
            {
                _syntaxNodes = new List<SyntaxNode>();
            }

            public IReadOnlyList<SyntaxNode> TagHelperNodes => _syntaxNodes;

            public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
            {
                _syntaxNodes.Add(node.Name);
                base.VisitMarkupTagHelperStartTag(node);
            }

            public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
            {
                _syntaxNodes.Add(node.Name);
                base.VisitMarkupTagHelperEndTag(node);
            }

            public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    _syntaxNodes.Add(node.Name);
                }

                base.VisitMarkupMinimizedTagHelperAttribute(node);
            }

            public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    _syntaxNodes.Add(node.Name);
                }

                base.VisitMarkupTagHelperAttribute(node);
            }
        }
    }
}
