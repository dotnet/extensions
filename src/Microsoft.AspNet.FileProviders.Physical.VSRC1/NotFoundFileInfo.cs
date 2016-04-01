// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.FileProviders.VSRC1
{
    /// <summary>
    /// Represents a non-existing file.
    /// </summary>
    internal class NotFoundFileInfo : IFileInfo
    {
        private readonly string _name;

        public NotFoundFileInfo(string name)
        {
            _name = name;
        }

        public bool Exists
        {
            get { return false; }
        }

        public bool IsDirectory
        {
            get { return false; }
        }

        public DateTimeOffset LastModified
        {
            get { return DateTimeOffset.MinValue; }
        }

        public long Length
        {
            get { return -1; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string PhysicalPath
        {
            get { return null; }
        }

        public Stream CreateReadStream()
        {
            throw new FileNotFoundException(string.Format("The file {0} does not exist.", Name));
        }
    }
}