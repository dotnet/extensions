// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.FileProviders.Physical
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
            // Note: Buffer size must be greater than zero, even if the file size is zero.
            return new FileStream(
                PhysicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                1024 * 64,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}
