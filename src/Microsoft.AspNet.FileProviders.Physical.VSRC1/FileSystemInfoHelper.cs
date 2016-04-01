// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNet.FileProviders.VSRC1
{
    public class FileSystemInfoHelper
    {
        internal static bool IsHiddenFile(FileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo.Name.StartsWith("."))
            {
                return true;
            }
            else if (fileSystemInfo.Exists &&
                (fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                fileSystemInfo.Attributes.HasFlag(FileAttributes.System)))
            {
                return true;
            }

            return false;
        }
    }
}