// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class IndentationContext
    {
        public int Line { get; set; }

        public int RazorIndentationLevel { get; set; }

        public int HtmlIndentationLevel { get; set; }

        public int IndentationLevel => RazorIndentationLevel + HtmlIndentationLevel;

        public int RelativeIndentationLevel { get; set; }

        public int ExistingIndentation { get; set; }

        public FormattingSpan FirstSpan { get; set; }

        public bool EmptyOrWhitespaceLine { get; set; }

        public bool StartsInHtmlContext => FirstSpan.Kind == FormattingSpanKind.Markup;

        public bool StartsInCSharpContext => FirstSpan.Kind == FormattingSpanKind.Code;

        public bool StartsInRazorContext => !StartsInHtmlContext && !StartsInCSharpContext;

        public int MinCSharpIndentLevel => FirstSpan.MinCSharpIndentLevel;

        public override string ToString()
        {
            return $"Line: {Line}, IndentationLevel: {IndentationLevel}, RelativeIndentationLevel: {RelativeIndentationLevel}, ExistingIndentation: {ExistingIndentation}";
        }
    }
}
