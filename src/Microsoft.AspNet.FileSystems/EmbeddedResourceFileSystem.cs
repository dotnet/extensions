// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.FileSystems
{
    /// <summary>
    /// Looks up files using embedded resources in the specified assembly.
    /// This file system is case sensitive.
    /// </summary>
    public class EmbeddedResourceFileSystem : IFileSystem
    {
        private readonly Assembly _assembly;
        private readonly string _baseNamespace;
        private readonly DateTime _lastModified;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResourceFileSystem" /> class using the specified
        /// assembly and empty base namespace.
        /// </summary>
        /// <param name="assembly"></param>
        public EmbeddedResourceFileSystem(Assembly assembly)
            : this(assembly, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResourceFileSystem" /> class using the specified
        /// assembly and base namespace.
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded resources.</param>
        /// <param name="baseNamespace">The base namespace that contains the embedded resources.</param>
        public EmbeddedResourceFileSystem(Assembly assembly, string baseNamespace)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            // Note: For ProjectK resources don't have a namespace anymore, just a directory path. Use '/' instead of '.'.
            _baseNamespace = string.IsNullOrEmpty(baseNamespace) ? string.Empty : baseNamespace + "/";
            _assembly = assembly;
            // REVIEW: Does this even make sense?
            _lastModified = DateTime.MaxValue;
        }

        /// <summary>
        /// Locates a file at the given path.
        /// </summary>
        /// <param name="subpath">The path that identifies the file. </param>
        /// <returns>The file information. Caller must check Exists property.</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            // Relative paths starting with a leading slash okay
            if (subpath.StartsWith("/", StringComparison.Ordinal))
            {
                subpath = subpath.Substring(1);
            }

            string resourcePath = _baseNamespace + subpath;
            string name = Path.GetFileName(subpath);
            if (_assembly.GetManifestResourceInfo(resourcePath) == null)
            {
                return new NotFoundFileInfo(name);
            }
            return new EmbeddedResourceFileInfo(_assembly, resourcePath, name, _lastModified);
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.		
        /// This file system uses a flat directory structure. Everything under the base namespace is considered to be one directory.		
        /// </summary>		
        /// <param name="subpath">The path that identifies the directory</param>		
        /// <returns>Contents of the directory. Caller must check Exists property.</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            // The file name is assumed to be the remainder of the resource name.
            if (subpath == null)
            {
                return new NotFoundDirectoryContents();
            }

            // Relative paths starting with a leading slash okay
            if (subpath.StartsWith("/", StringComparison.Ordinal))
            {
                subpath = subpath.Substring(1);
            }

            // Non-hierarchal.
            if (!subpath.Equals(string.Empty))
            {
                return new NotFoundDirectoryContents();
            }

            IList<IFileInfo> entries = new List<IFileInfo>();

            // TODO: The list of resources in an assembly isn't going to change. Consider caching.
            string[] resources = _assembly.GetManifestResourceNames();
            for (int i = 0; i < resources.Length; i++)
            {
                string resourceName = resources[i];
                if (resourceName.StartsWith(_baseNamespace))
                {
                    entries.Add(new EmbeddedResourceFileInfo(
                        _assembly, resourceName, resourceName.Substring(_baseNamespace.Length), _lastModified));
                }
            }

            return new EnumerableDirectoryContents(entries);
        }

        private class EmbeddedResourceFileInfo : IFileInfo
        {
            private readonly Assembly _assembly;
            private readonly DateTime _lastModified;
            private readonly string _resourcePath;
            private readonly string _name;

            private long? _length;

            public EmbeddedResourceFileInfo(Assembly assembly, string resourcePath, string name, DateTime lastModified)
            {
                _assembly = assembly;
                _lastModified = lastModified;
                _resourcePath = resourcePath;
                _name = name;
            }

            public bool Exists
            {
                get { return true; }
            }

            public long Length
            {
                get
                {
                    if (!_length.HasValue)
                    {
                        using (Stream stream = _assembly.GetManifestResourceStream(_resourcePath))
                        {
                            _length = stream.Length;
                        }
                    }
                    return _length.Value;
                }
            }

            // Not directly accessible.
            public string PhysicalPath
            {
                get { return null; }
            }

            public string Name
            {
                get { return _name; }
            }

            public DateTime LastModified
            {
                get { return _lastModified; }
            }

            public bool IsDirectory
            {
                get { return false; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public Stream CreateReadStream()
            {
                Stream stream = _assembly.GetManifestResourceStream(_resourcePath);
                if (!_length.HasValue)
                {
                    _length = stream.Length;
                }
                return stream;
            }

            public void WriteContent(byte[] content)
            {
                throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(EmbeddedResourceFileSystem), nameof(WriteContent)));
            }

            public void Delete()
            {
                throw new InvalidOperationException(string.Format("{0} does not support {1}.", nameof(EmbeddedResourceFileSystem), nameof(Delete)));
            }

            public IExpirationTrigger CreateFileChangeTrigger()
            {
                throw new NotSupportedException(string.Format("{0} does not support {1}.", nameof(EmbeddedResourceFileSystem), nameof(CreateFileChangeTrigger)));
            }
        }
    }
}
