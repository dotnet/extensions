// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class RazorLanguageKindExtensions
    {
        public static string ToContainedLanguageContentType(this RazorLanguageKind razorLanguageKind) =>
            razorLanguageKind == RazorLanguageKind.CSharp ? RazorLSPConstants.CSharpContentTypeName : RazorLSPConstants.HtmlLSPContentTypeName;
    }
}
