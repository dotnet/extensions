// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.FileSystems
{
    /// <summary>
    /// Represents a non-existing file.
    /// </summary>
    public class NotFoundFileInfo : IFileInfo
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

        public DateTime LastModified
        {
            get { return DateTime.MinValue; }
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

        public bool IsReadOnly
        {
            get
            {
                throw new InvalidOperationException(string.Format("The file {0} does not exist.", Name));
            }
        }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException(string.Format("The file {0} does not exist.", Name));
        }

        public void WriteContent(byte[] content)
        {
            throw new InvalidOperationException(string.Format("The file {0} does not exist.", Name));
        }

        public void Delete()
        {
            throw new InvalidOperationException(string.Format("The file {0} does not exist.", Name));
        }
    }
}