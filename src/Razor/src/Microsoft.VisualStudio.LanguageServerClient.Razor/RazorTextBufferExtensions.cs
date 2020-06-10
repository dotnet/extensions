// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServerClient.Razor;

namespace Microsoft.VisualStudio.Text
{
    internal static class RazorTextBufferExtensions
    {
        public static bool IsRazorLSPBuffer(this ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            var matchesContentType = textBuffer.ContentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName);
            return matchesContentType;
        }
    }
}
