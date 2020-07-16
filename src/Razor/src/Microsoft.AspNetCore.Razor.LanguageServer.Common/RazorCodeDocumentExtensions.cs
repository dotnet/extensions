// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal static class RazorCodeDocumentExtensions
    {
        private static readonly object UnsupportedKey = new object();
        private static readonly object SourceTextKey = new object();

        public static bool IsUnsupported(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var unsupportedObj = document.Items[UnsupportedKey];
            if (unsupportedObj == null)
            {
                return false;
            }

            return (bool)unsupportedObj;
        }

        public static void SetUnsupported(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[UnsupportedKey] = true;
        }

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
    }
}
