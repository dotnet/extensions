// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CloseTextTagFormatOnTypeProvider : RazorFormatOnTypeProvider
    {
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;

        public CloseTextTagFormatOnTypeProvider(IOptionsMonitor<RazorLSPOptions> optionsMonitor)
        {
            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _optionsMonitor = optionsMonitor;
        }

        public override string TriggerCharacter => ">";

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

            if (!_optionsMonitor.CurrentValue.AutoClosingTags)
            {
                // We currently only support auto-closing tags our onType formatter.
                edits = null;
                return false;
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
                // Can remove this once VSCode starts using this endpoint for auto closing <text> tags.
                edits = null;
                return false;
            }

            if (!IsAtTextTag(context, position))
            {
                edits = null;
                return false;
            }

            // This is a text tag.
            var cursorPlaceholder = addCursorPlaceholder ? LanguageServerConstants.CursorPlaceholderString : string.Empty;
            var edit = new TextEdit()
            {
                NewText = $"{cursorPlaceholder}</{SyntaxConstants.TextTagName}>",
                Range = new Range(position, position)
            };

            edits = new[] { edit };
            return true;
        }

        private static bool IsAtTextTag(FormattingContext context, Position position)
        {
            var syntaxTree = context.CodeDocument.GetSyntaxTree();

            var absoluteIndex = position.GetAbsoluteIndex(context.SourceText) - 1;
            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner?.Parent != null &&
                owner.Parent is MarkupStartTagSyntax startTag &&
                startTag.IsMarkupTransition &&
                startTag.Parent is MarkupElementSyntax element &&
                element.EndTag == null) // Make sure the end </text> tag doesn't already exist
            {
                Debug.Assert(string.Equals(startTag.Name.Content, SyntaxConstants.TextTagName, StringComparison.Ordinal), "MarkupTransition that is not a <text> tag.");

                return true;
            }

            return false;
        }
    }
}
