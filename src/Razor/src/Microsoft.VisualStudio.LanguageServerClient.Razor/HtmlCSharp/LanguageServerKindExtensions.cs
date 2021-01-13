// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal static class LanguageServerKindExtensions
    {
        public static string ToContentType(this LanguageServerKind languageServerKind)
        {
            switch (languageServerKind)
            {
                case LanguageServerKind.CSharp:
                    return RazorLSPConstants.CSharpContentTypeName;
                case LanguageServerKind.Html:
                    return RazorLSPConstants.HtmlLSPContentTypeName;
                default:
                    return RazorLSPConstants.RazorLSPContentTypeName;
            }
        }
    }
}
