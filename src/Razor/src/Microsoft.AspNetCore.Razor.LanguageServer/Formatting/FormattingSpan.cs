// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingSpan
    {
        public FormattingSpan(TextSpan span, TextSpan blockSpan, FormattingSpanKind spanKind, FormattingBlockKind blockKind, int indentationLevel, bool isInClassBody = false)
        {
            Span = span;
            BlockSpan = blockSpan;
            Kind = spanKind;
            BlockKind = blockKind;
            IndentationLevel = indentationLevel;
            IsInClassBody = isInClassBody;
        }

        public TextSpan Span { get; }

        public TextSpan BlockSpan { get; }

        public FormattingBlockKind BlockKind { get; }

        public FormattingSpanKind Kind { get; }

        public int IndentationLevel { get; }

        public bool IsInClassBody { get; }
    }
}
