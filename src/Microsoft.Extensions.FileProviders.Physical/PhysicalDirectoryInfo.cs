// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class PhysicalDirectoryInfo : IFileInfo
    {
        private readonly DirectoryInfo _info;

        public PhysicalDirectoryInfo(DirectoryInfo info)
        {
            _info = info;
        }

        public bool Exists => _info.Exists;

        public long Length => -1;

        public string PhysicalPath => _info.FullName;

        public string Name => _info.Name;

        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Cannot create a stream for a directory.");
        }
    }
}
