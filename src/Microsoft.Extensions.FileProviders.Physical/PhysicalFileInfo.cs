// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class PhysicalFileInfo : IFileInfo
    {
        private readonly FileInfo _info;

        public PhysicalFileInfo(FileInfo info)
        {
            _info = info;
        }

        public bool Exists => _info.Exists;

        public long Length => _info.Length;

        public string PhysicalPath => _info.FullName;

        public string Name => _info.Name;

        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
            // 0 causes constructor to throw
            var bufferSize = 1;
            return new FileStream(
                PhysicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}
