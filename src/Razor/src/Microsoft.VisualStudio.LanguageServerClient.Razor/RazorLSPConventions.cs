// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class RazorLSPConventions
    {
        public static bool IsRazorCSharpFile(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return uri.GetAbsoluteOrUNCPath()?.EndsWith(RazorLSPConstants.VirtualCSharpFileNameSuffix, StringComparison.Ordinal) ?? false;
        }

        public static bool IsRazorHtmlFile(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return uri.GetAbsoluteOrUNCPath()?.EndsWith(RazorLSPConstants.VirtualHtmlFileNameSuffix, StringComparison.Ordinal) ?? false;
        }

        public static Uri GetRazorDocumentUri(Uri virtualDocumentUri)
        {
            if (virtualDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(virtualDocumentUri));
            }

            var path = virtualDocumentUri.AbsoluteUri;
            path = path.Replace(RazorLSPConstants.VirtualCSharpFileNameSuffix, string.Empty);
            path = path.Replace(RazorLSPConstants.VirtualHtmlFileNameSuffix, string.Empty);

            var uri = new Uri(path, UriKind.Absolute);
            return uri;
        }
    }
}
