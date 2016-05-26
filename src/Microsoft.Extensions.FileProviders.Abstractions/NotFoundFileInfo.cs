// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a non-existing file.
    /// </summary>
    public class NotFoundFileInfo : IFileInfo
    {
        public NotFoundFileInfo(string name)
        {
            Name = name;
        }

        public bool Exists => false;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified => DateTimeOffset.MinValue;

        public long Length => -1;

        public string Name { get; }

        public string PhysicalPath => null;

        public Stream CreateReadStream()
        {
            throw new FileNotFoundException($"The file {Name} does not exist.");
        }
    }
}