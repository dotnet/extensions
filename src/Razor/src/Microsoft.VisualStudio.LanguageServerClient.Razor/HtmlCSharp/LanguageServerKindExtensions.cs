// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal static class LanguageServerKindExtensions
    {
        public static string ToContentType(this LanguageServerKind languageServerKind)
        {
            return languageServerKind switch
            {
                LanguageServerKind.CSharp => RazorLSPConstants.CSharpContentTypeName,
                LanguageServerKind.Html => RazorLSPConstants.HtmlLSPContentTypeName,
                _ => RazorLSPConstants.RazorLSPContentTypeName,
            };
        }
    }
}
