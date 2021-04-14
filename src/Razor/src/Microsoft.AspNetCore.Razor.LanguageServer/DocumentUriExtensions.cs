// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class DocumentUriExtensions
    {
        public static string GetAbsoluteOrUNCPath(this DocumentUri documentUri)
        {
            if (documentUri is null)
            {
                throw new ArgumentNullException(nameof(documentUri));
            }

            return documentUri.ToUri().GetAbsoluteOrUNCPath();
        }

        public static string GetAbsolutePath(this DocumentUri documentUri)
        {
            if (documentUri is null)
            {
                throw new ArgumentNullException(nameof(documentUri));
            }

            return documentUri.ToUri().AbsolutePath;
        }
    }
}
