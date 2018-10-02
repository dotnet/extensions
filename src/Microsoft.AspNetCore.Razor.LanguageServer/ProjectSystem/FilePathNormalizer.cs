// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    public sealed class FilePathNormalizer
    {
        public string Normalize(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return "/";
            }

            var decodedPath = WebUtility.UrlDecode(filePath);
            var normalized = decodedPath.Replace('\\', '/');

            if (normalized[0] != '/')
            {
                normalized = '/' + normalized;
            }

            return normalized;
        }

        public string GetDirectory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new InvalidOperationException(filePath);
            }

            var normalizedPath = Normalize(filePath);
            var lastSeparatorIndex = normalizedPath.LastIndexOf('/');

            var directory = normalizedPath.Substring(0, lastSeparatorIndex + 1);
            return directory;
        }
    }
}
