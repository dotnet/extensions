// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical.Internal
{
    /// <summary>
    /// Helpful methods for FileSystem APIs.
    /// </summary>
    public static class FileSystemInfoHelper
    {
        /// <summary>
        /// Determines if a file or directory should be excluded based on given <see cref="ExclusionFilters"/>.
        /// </summary>
        /// <param name="fileSystemInfo">The file or directory info.</param>
        /// <param name="filters">The exclusion filter.</param>
        /// <returns>True when the file or directory should be filtered.</returns>
        public static bool IsExcluded(FileSystemInfo fileSystemInfo, ExclusionFilters filters)
        {
            if (filters == ExclusionFilters.None)
            {
                return false;
            }
            else if (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != 0)
            {
                return true;
            }
            else if (fileSystemInfo.Exists &&
                (((fileSystemInfo.Attributes & FileAttributes.Hidden) != 0 && (filters & ExclusionFilters.Hidden) != 0) ||
                 ((fileSystemInfo.Attributes & FileAttributes.System) != 0 && (filters & ExclusionFilters.System) != 0)))
            {
                return true;
            }

            return false;
        }
    }
}
