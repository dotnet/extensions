// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    internal class CloseRazorCommentOnAutoInsertProvider : RazorOnAutoInsertProvider
    {
        public override string TriggerCharacter => "*";

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

            if (!IsAtRazorCommentStart(context, position))
            {
                format = default;
                edit = default;
                return false;
            }

            // We've just typed a Razor comment start.
            format = InsertTextFormat.Snippet;
            edit = new TextEdit()
            {
                NewText = " $0 *@",
                Range = new Range(position, position)
            };

            return true;
        }

        private static bool IsAtRazorCommentStart(FormattingContext context, Position position)
        {
            var syntaxTree = context.CodeDocument.GetSyntaxTree();

            var absoluteIndex = position.GetAbsoluteIndex(context.SourceText);
            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            return owner != null &&
                owner.Kind == SyntaxKind.RazorCommentStar &&
                owner.Parent is RazorCommentBlockSyntax comment &&
                owner.Position == comment.StartCommentStar.Position;
        }
    }
}
