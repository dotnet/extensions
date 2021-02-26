// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingSpan
    {
        public FormattingSpan(
            TextSpan span,
            TextSpan blockSpan,
            FormattingSpanKind spanKind,
            FormattingBlockKind blockKind,
            int razorIndentationLevel,
            int htmlIndentationLevel,
            bool isInClassBody = false,
            int componentLambdaNestingLevel = 0)
        {
            Span = span;
            BlockSpan = blockSpan;
            Kind = spanKind;
            BlockKind = blockKind;
            RazorIndentationLevel = razorIndentationLevel;
            HtmlIndentationLevel = htmlIndentationLevel;
            IsInClassBody = isInClassBody;
            ComponentLambdaNestingLevel = componentLambdaNestingLevel;
        }

        public TextSpan Span { get; }

        public TextSpan BlockSpan { get; }

        public FormattingBlockKind BlockKind { get; }

        public FormattingSpanKind Kind { get; }

        public int RazorIndentationLevel { get; }

        public int HtmlIndentationLevel { get; }

        public int IndentationLevel => RazorIndentationLevel + HtmlIndentationLevel;

        public bool IsInClassBody { get; }

        public int ComponentLambdaNestingLevel { get; }

        public int MinCSharpIndentLevel
        {
            get
            {
                if (IsInClassBody)
                {
                    return 2;
                }
                else
                {
                    return 3 + ComponentLambdaNestingLevel;
                }
            }
        }
    }
}
