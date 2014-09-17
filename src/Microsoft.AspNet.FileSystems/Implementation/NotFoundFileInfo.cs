// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.FileSystems
{
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
                throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(NotFoundFileInfo), nameof(IsReadOnly)));
            }
        }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(NotFoundFileInfo), nameof(CreateReadStream)));
        }

        public void WriteContent(byte[] content)
        {
            throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(NotFoundFileInfo), nameof(WriteContent)));
        }

        public void Delete()
        {
            throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(NotFoundFileInfo), nameof(Delete)));
        }

        public IExpirationTrigger CreateFileChangeTrigger()
        {
            throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(NotFoundFileInfo), nameof(CreateFileChangeTrigger)));
        }
    }
}