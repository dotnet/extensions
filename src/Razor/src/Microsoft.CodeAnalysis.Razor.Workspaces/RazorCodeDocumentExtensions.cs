// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    internal static class RazorCodeDocumentExtensions
    {
        private static readonly object SourceTextKey = new object();
        private static readonly object CSharpSourceTextKey = new object();
        private static readonly object HtmlSourceTextKey = new object();

        public static SourceText GetSourceText(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var sourceTextObj = document.Items[SourceTextKey];
            if (sourceTextObj == null)
            {
                var source = document.Source;
                var charBuffer = new char[source.Length];
                source.CopyTo(0, charBuffer, 0, source.Length);
                var sourceText = SourceText.From(new string(charBuffer));
                document.Items[SourceTextKey] = sourceText;

                return sourceText;
            }

            return (SourceText)sourceTextObj;
        }

        public static SourceText GetCSharpSourceText(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var sourceTextObj = document.Items[CSharpSourceTextKey];
            if (sourceTextObj == null)
            {
                var csharpDocument = document.GetCSharpDocument();
                var sourceText = SourceText.From(csharpDocument.GeneratedCode);
                document.Items[CSharpSourceTextKey] = sourceText;

                return sourceText;
            }

            return (SourceText)sourceTextObj;
        }

        public static SourceText GetHtmlSourceText(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var sourceTextObj = document.Items[HtmlSourceTextKey];
            if (sourceTextObj == null)
            {
                var htmlDocument = document.GetHtmlDocument();
                var sourceText = SourceText.From(htmlDocument.GeneratedHtml);
                document.Items[HtmlSourceTextKey] = sourceText;

                return sourceText;
            }

            return (SourceText)sourceTextObj;
        }
    }
}
