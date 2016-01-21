// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.FileProviders.Embedded
{
    public class EmbeddedResourceFileInfo : IFileInfo
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePath;

        private long? _length;

        public EmbeddedResourceFileInfo(
            Assembly assembly,
            string resourcePath,
            string name,
            DateTimeOffset lastModified)
        {
            _assembly = assembly;
            _resourcePath = resourcePath;
            Name = name;
            LastModified = lastModified;
        }

        public bool Exists => true;

        public long Length
        {
            get
            {
                if (!_length.HasValue)
                {
                    using (var stream = _assembly.GetManifestResourceStream(_resourcePath))
                    {
                        _length = stream.Length;
                    }
                }
                return _length.Value;
            }
        }

        // Not directly accessible.
        public string PhysicalPath => null;

        public string Name { get; }

        public DateTimeOffset LastModified { get; }

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            var stream = _assembly.GetManifestResourceStream(_resourcePath);
            if (!_length.HasValue)
            {
                _length = stream.Length;
            }
            return stream;
        }
    }
}
