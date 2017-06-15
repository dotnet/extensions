// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    internal static class FileSystemInfoHelper
    {
        public static bool IsHiddenFile(FileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo.Name.StartsWith("."))
            {
                return true;
            }
            else if (fileSystemInfo.Exists &&
                ((fileSystemInfo.Attributes & FileAttributes.Hidden) != 0 ||
                 (fileSystemInfo.Attributes & FileAttributes.System) != 0))
            {
                return true;
            }

            return false;
        }
    }
}
