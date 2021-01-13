// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class UriExtensions
    {
        public static string GetAbsoluteOrUNCPath(this Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.IsUnc)
            {
                // For UNC paths, AbsolutePath doesn't include the host name `//COMPUTERNAME/` part. So we need to use LocalPath instead.
                return uri.LocalPath;
            }

            // Absolute paths are usually encoded.
            return WebUtility.UrlDecode(uri.AbsolutePath);
        }
    }
}
