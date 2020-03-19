// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
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

            var syntaxRanges = VisitAllNodes(codeDocument);

            var semanticTokens = ConvertSyntaxTokensToSemanticTokens(syntaxRanges, codeDocument);

            return semanticTokens;
        }

        private static IReadOnlyList<SyntaxResult> VisitAllNodes(RazorCodeDocument razorCodeDocument)
        {
            var visitor = new TagHelperSpanVisitor(razorCodeDocument);
            visitor.Visit(razorCodeDocument.GetSyntaxTree().Root);

            return visitor.TagHelperData;
        }

        private static SemanticTokens ConvertSyntaxTokensToSemanticTokens(
            IEnumerable<SyntaxResult> syntaxResults,
            RazorCodeDocument razorCodeDocument)
        {
            SyntaxResult? previousResult = null;

            var data = new List<uint>();
            foreach (var result in syntaxResults)
            {
                var newData = GetData(result, previousResult, razorCodeDocument);
                data.AddRange(newData);

                previousResult = result;
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
            SyntaxResult currentNode,
            SyntaxResult? previousNode,
            RazorCodeDocument razorCodeDocument)
        {
            var previousRange = previousNode?.Range;
            var currentRange = currentNode.Range;

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
                yield return (uint)currentRange.Start.Character;
            }

            // length
            var endIndex = currentNode.Range.End.GetAbsoluteIndex(razorCodeDocument.GetSourceText());
            var startIndex = currentNode.Range.Start.GetAbsoluteIndex(razorCodeDocument.GetSourceText());
            var length = endIndex - startIndex;
            Debug.Assert(length > 0);
            yield return (uint)length;

            // tokenType
            yield return GetTokenTypeData(currentNode.Kind);

            // tokenModifiers
            // We don't currently have any need for tokenModifiers
            yield return 0;
        }

        private static uint GetTokenTypeData(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.MarkupTagHelperDirectiveAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute:
                    return SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorDirectiveAttribute];
                case SyntaxKind.MarkupTagHelperStartTag:
                case SyntaxKind.MarkupTagHelperEndTag:
                    return SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorTagHelperElement];
                case SyntaxKind.MarkupTagHelperAttribute:
                case SyntaxKind.MarkupMinimizedTagHelperAttribute:
                    return SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorTagHelperAttribute];
                case SyntaxKind.Transition:
                    return SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorTransition];
                case SyntaxKind.Colon:
                    return SemanticTokenLegend.TokenTypesLegend[SemanticTokenLegend.RazorDirectiveColon];
                default:
                    throw new NotImplementedException();
            }
        }

        private struct SyntaxResult
        {
            public SyntaxResult(SyntaxNode node, SyntaxKind kind, RazorCodeDocument razorCodeDocument)
            {
                var range = node.GetRange(razorCodeDocument.Source);
                Range = range;
                Kind = kind;
            }

            public Range Range { get; set; }

            public SyntaxKind Kind { get; set; }

        }

        private class TagHelperSpanVisitor : SyntaxWalker
        {
            private readonly List<SyntaxResult> _syntaxNodes;
            private readonly RazorCodeDocument _razorCodeDocument;

            public TagHelperSpanVisitor(RazorCodeDocument razorCodeDocument)
            {
                _syntaxNodes = new List<SyntaxResult>();
                _razorCodeDocument = razorCodeDocument;
            }

            public IReadOnlyList<SyntaxResult> TagHelperData => _syntaxNodes;

            public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
            {
                if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
                {
                    var result = new SyntaxResult(node.Name, SyntaxKind.MarkupTagHelperStartTag, _razorCodeDocument);
                    _syntaxNodes.Add(result);
                }
                base.VisitMarkupTagHelperStartTag(node);
            }

            public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
            {
                if (ClassifyTagName((MarkupTagHelperElementSyntax)node.Parent))
                {
                    var result = new SyntaxResult(node.Name, SyntaxKind.MarkupTagHelperEndTag, _razorCodeDocument);
                    _syntaxNodes.Add(result);
                }
                base.VisitMarkupTagHelperEndTag(node);
            }

            public override void VisitMarkupMinimizedTagHelperAttribute(MarkupMinimizedTagHelperAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    var result = new SyntaxResult(node.Name, SyntaxKind.MarkupMinimizedTagHelperAttribute, _razorCodeDocument);
                    _syntaxNodes.Add(result);
                }

                base.VisitMarkupMinimizedTagHelperAttribute(node);
            }

            public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    var result = new SyntaxResult(node.Name, SyntaxKind.MarkupTagHelperAttribute, _razorCodeDocument);
                    _syntaxNodes.Add(result);
                }

                base.VisitMarkupTagHelperAttribute(node);
            }

            public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    var transition = new SyntaxResult(node.Transition, SyntaxKind.Transition, _razorCodeDocument);
                    _syntaxNodes.Add(transition);

                    var directiveAttribute = new SyntaxResult(node.Name, SyntaxKind.MarkupTagHelperDirectiveAttribute, _razorCodeDocument);
                    _syntaxNodes.Add(directiveAttribute);

                    if (node.Colon != null)
                    {
                        var colon = new SyntaxResult(node.Colon, SyntaxKind.Colon, _razorCodeDocument);
                        _syntaxNodes.Add(colon);
                    }

                    if (node.ParameterName != null)
                    {
                        var parameterName = new SyntaxResult(node.ParameterName, SyntaxKind.MarkupTagHelperDirectiveAttribute, _razorCodeDocument);
                        _syntaxNodes.Add(parameterName);
                    }
                }

                base.VisitMarkupTagHelperDirectiveAttribute(node);
            }

            public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
            {
                if (node.TagHelperAttributeInfo.Bound)
                {
                    var transition = new SyntaxResult(node.Transition, SyntaxKind.Transition, _razorCodeDocument);
                    _syntaxNodes.Add(transition);

                    var directiveAttribute = new SyntaxResult(node.Name, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute, _razorCodeDocument);
                    _syntaxNodes.Add(directiveAttribute);

                    if (node.Colon != null)
                    {
                        var colon = new SyntaxResult(node.Colon, SyntaxKind.Colon, _razorCodeDocument);
                        _syntaxNodes.Add(colon);
                    }

                    if (node.ParameterName != null)
                    {
                        var parameterName = new SyntaxResult(node.ParameterName, SyntaxKind.MarkupMinimizedTagHelperDirectiveAttribute, _razorCodeDocument);
                        _syntaxNodes.Add(parameterName);
                    }
                }

                base.VisitMarkupMinimizedTagHelperDirectiveAttribute(node);
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
        }
    }
}
