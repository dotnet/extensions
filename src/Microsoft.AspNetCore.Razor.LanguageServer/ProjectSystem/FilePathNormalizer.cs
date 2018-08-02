// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    public sealed class FilePathNormalizer
    {
        public string Normalize(string filePath)
        {
            if (filePath[0] == '/')
            {
                filePath = filePath.Substring(1);
            }

            var decodedPath = WebUtility.UrlDecode(filePath);
            var normalized = decodedPath.Replace('\\', '/');
            return normalized;
        }
    }
}
