// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    internal class HtmlSmartIndentOnAutoInsertProvider : RazorOnAutoInsertProvider
    {
        public override string TriggerCharacter => "\n";

        public override bool TryResolveInsertion(Position position, FormattingContext context, out TextEdit edit, out InsertTextFormat format)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var syntaxTree = context.CodeDocument.GetSyntaxTree();

            var absoluteIndex = position.GetAbsoluteIndex(context.SourceText);
            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (!IsAtEnterRuleLocation(context, owner))
            {
                format = default;
                edit = default;
                return false;
            }

            // We're currently at:
            // <someTag>
            // |</someTag>

            context.SourceText.GetLineAndOffset(owner.SpanStart, out var lineNumber, out _);

            var existingIndentation = context.Indentations[lineNumber].ExistingIndentation;
            var existingIndentationString = context.GetIndentationString(existingIndentation);
            var increasedIndentationString = context.GetIndentationLevelString(indentationLevel: 1);
            var innerIndentationString = string.Concat(increasedIndentationString, existingIndentationString);

            // We mark start position at the beginning of the line in order to remove any pre-existing whitespace.
            var startPosition = new Position(position.Line, 0);
            format = InsertTextFormat.Snippet;
            edit = new TextEdit()
            {
                NewText = $"{innerIndentationString}$0{Environment.NewLine}{existingIndentationString}",
                Range = new Range(startPosition, position)
            };
            return true;
        }

        private static bool IsAtEnterRuleLocation(FormattingContext context, SyntaxNode owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (!TryGetApplicableBody(owner, out var body))
            {
                return false;
            }

            if (!IsApplicableElementBody(body))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetApplicableBody(SyntaxNode owner, out SyntaxList<RazorSyntaxNode> body)
        {
            if (TryGetApplicableTagBody(owner, out body))
            {
                return true;
            }

            if (TryGetApplicableTagHelperBody(owner, out body))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetApplicableTagBody(SyntaxNode owner, out SyntaxList<RazorSyntaxNode> body)
        {
            var parent = owner.Parent;
            if (parent.Kind != SyntaxKind.MarkupElement)
            {
                return false;
            }

            var markupElement = (MarkupElementSyntax)parent;
            if (markupElement.StartTag == null)
            {
                return false;
            }

            if (markupElement.EndTag == null)
            {
                return false;
            }

            body = markupElement.Body;
            return true;
        }

        private static bool TryGetApplicableTagHelperBody(SyntaxNode owner, out SyntaxList<RazorSyntaxNode> body)
        {
            var parent = owner.Parent;
            if (parent.Kind != SyntaxKind.MarkupTagHelperElement)
            {
                return false;
            }

            var tagHelperElement = (MarkupTagHelperElementSyntax)parent;
            if (tagHelperElement.StartTag == null)
            {
                return false;
            }

            if (tagHelperElement.EndTag == null)
            {
                return false;
            }

            body = tagHelperElement.Body;
            return true;
        }

        private static bool IsApplicableElementBody(SyntaxList<RazorSyntaxNode> body)
        {
            for (var i = 0; i < body.Count; i++)
            {
                var child = body[i];
                if (child.Kind != SyntaxKind.MarkupTextLiteral)
                {
                    return false;
                }

                var textLiteral = (MarkupTextLiteralSyntax)child;
                var literalTokens = textLiteral.LiteralTokens;
                var newlineCount = 0;

                for (var j = 0; j < literalTokens.Count; j++)
                {
                    var tokenKind = literalTokens[j].Kind;

                    if (tokenKind == SyntaxKind.NewLine)
                    {
                        if (newlineCount++ > 0)
                        {
                            // More than one newline in the body, we're not going to do anything.
                            return false;
                        }

                        continue;
                    }

                    if (tokenKind != SyntaxKind.Whitespace)
                    {
                        // Non-whitespace and non-newline character
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
