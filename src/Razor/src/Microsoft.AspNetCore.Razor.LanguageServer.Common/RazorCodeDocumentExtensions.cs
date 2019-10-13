// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal static class RazorCodeDocumentExtensions
    {
        private static readonly object UnsupportedKey = new object();

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
    }
}
