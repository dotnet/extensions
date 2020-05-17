// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CloseRazorCommentFormatOnTypeProvider : RazorFormatOnTypeProvider
    {
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;

        public CloseRazorCommentFormatOnTypeProvider(IOptionsMonitor<RazorLSPOptions> optionsMonitor)
        {
            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _optionsMonitor = optionsMonitor;
        }

        public override string TriggerCharacter => "*";

        public override bool TryFormatOnType(Position position, FormattingContext context, out TextEdit[] edits)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            bool addCursorPlaceholder;
            if (context.Options.TryGetValue(LanguageServerConstants.ExpectsCursorPlaceholderKey, out var value) && value.IsBool)
            {
                addCursorPlaceholder = value.Bool;
            }
            else
            {
                // Temporary:
                // no-op if cursor placeholder isn't supported. This means the request isn't coming from VS.
                edits = null;
                return false;
            }

            if (!IsAtRazorCommentStart(context, position))
            {
                edits = null;
                return false;
            }

            // We've just typed a Razor comment start.
            var cursorPlaceholder = addCursorPlaceholder ? LanguageServerConstants.CursorPlaceholderString : string.Empty;
            var edit = new TextEdit()
            {
                NewText = $" {cursorPlaceholder} *@",
                Range = new Range(position, position)
            };

            edits = new[] { edit };
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
