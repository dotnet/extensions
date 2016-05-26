// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Looks up files using the on-disk file system
    /// </summary>
    public class PhysicalFileProvider : IFileProvider, IDisposable
    {
        private const string PollingEnvironmentKey = "DOTNET_USE_POLLING_FILE_WATCHER";
        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
            .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();
        private static readonly char[] _pathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly PhysicalFilesWatcher _filesWatcher;

        /// <summary>
        /// Creates a new instance of a PhysicalFileProvider at the given root directory.
        /// </summary>
        /// <param name="root">The root directory. This should be an absolute path.</param>
        public PhysicalFileProvider(string root)
            : this(root, CreateFileWatcher(root))
        {
        }

        internal PhysicalFileProvider(string root, PhysicalFilesWatcher physicalFilesWatcher)
        {
            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException("The path must be absolute.", nameof(root));
            }
            var fullRoot = Path.GetFullPath(root);
            // When we do matches in GetFullPath, we want to only match full directory names.
            Root = EnsureTrailingSlash(fullRoot);
            if (!Directory.Exists(Root))
            {
                throw new DirectoryNotFoundException(Root);
            }

            _filesWatcher = physicalFilesWatcher;
        }

        private static PhysicalFilesWatcher CreateFileWatcher(string root)
        {
            var environmentValue = Environment.GetEnvironmentVariable(PollingEnvironmentKey);
            var pollForChanges = string.Equals(environmentValue, "1", StringComparison.Ordinal) ||
                string.Equals(environmentValue, "true", StringComparison.OrdinalIgnoreCase);

            root = EnsureTrailingSlash(Path.GetFullPath(root));
            return new PhysicalFilesWatcher(root, new FileSystemWatcher(root), pollForChanges);
        }

        public void Dispose()
        {
            _filesWatcher.Dispose();
        }

        /// <summary>
        /// The root directory for this instance.
        /// </summary>
        public string Root { get; }

        private string GetFullPath(string path)
        {
            if (PathNavigatesAboveRoot(path))
            {
                return null;
            }

            var fullPath = Path.GetFullPath(Path.Combine(Root, path));
            if (!IsUnderneathRoot(fullPath))
            {
                return null;
            }

            return fullPath;
        }

        private bool PathNavigatesAboveRoot(string path)
        {
            var tokenizer = new StringTokenizer(path, _pathSeparators);
            var depth = 0;

            foreach (var segment in tokenizer)
            {
                if (segment.Equals(".") || segment.Equals(""))
                {
                    continue;
                }
                else if (segment.Equals(".."))
                {
                    depth--;

                    if (depth == -1)
                    {
                        return true;
                    }
                }
                else
                {
                    depth++;
                }
            }

            return false;
        }

        private bool IsUnderneathRoot(string fullPath)
        {
            return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (!string.IsNullOrEmpty(path) &&
                path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        private static bool HasInvalidPathChars(string path)
        {
            return path.IndexOfAny(_invalidFileNameChars) != -1;
        }

        /// <summary>
        /// Locate a file at the given path by directly mapping path segments to physical directories.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>The file information. Caller must check Exists property. </returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath) || HasInvalidPathChars(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            // Relative paths starting with a leading slash okay
            if (subpath.StartsWith("/", StringComparison.Ordinal))
            {
                subpath = subpath.Substring(1);
            }

            // Absolute paths not permitted.
            if (Path.IsPathRooted(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            var fullPath = GetFullPath(subpath);
            if (fullPath == null)
            {
                return new NotFoundFileInfo(subpath);
            }

            var fileInfo = new FileInfo(fullPath);
            if (FileSystemInfoHelper.IsHiddenFile(fileInfo))
            {
                return new NotFoundFileInfo(subpath);
            }

            return new PhysicalFileInfo(fileInfo);
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>Contents of the directory. Caller must check Exists property.</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            try
            {
                if (subpath == null || HasInvalidPathChars(subpath))
                {
                    return new NotFoundDirectoryContents();
                }

                // Relative paths starting with a leading slash okay
                if (subpath.StartsWith("/", StringComparison.Ordinal))
                {
                    subpath = subpath.Substring(1);
                }

                // Absolute paths not permitted.
                if (Path.IsPathRooted(subpath))
                {
                    return new NotFoundDirectoryContents();
                }

                var fullPath = GetFullPath(subpath);
                if (fullPath != null)
                {
                    var directoryInfo = new DirectoryInfo(fullPath);
                    if (!directoryInfo.Exists)
                    {
                        return new NotFoundDirectoryContents();
                    }

                    var physicalInfos = directoryInfo
                        .EnumerateFileSystemInfos()
                        .Where(info => !FileSystemInfoHelper.IsHiddenFile(info));
                    var virtualInfos = new List<IFileInfo>();
                    foreach (var fileSystemInfo in physicalInfos)
                    {
                        var fileInfo = fileSystemInfo as FileInfo;
                        if (fileInfo != null)
                        {
                            virtualInfos.Add(new PhysicalFileInfo(fileInfo));
                        }
                        else
                        {
                            virtualInfos.Add(new PhysicalDirectoryInfo((DirectoryInfo)fileSystemInfo));
                        }
                    }

                    return new EnumerableDirectoryContents(virtualInfos);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (IOException)
            {
            }
            return new NotFoundDirectoryContents();
        }

        public IChangeToken Watch(string filter)
        {
            if (filter == null)
            {
                return NullChangeToken.Singleton;
            }

            if (PathNavigatesAboveRoot(filter))
            {
                return NullChangeToken.Singleton;
            }

            // Relative paths starting with a leading slash okay
            if (filter.StartsWith("/", StringComparison.Ordinal))
            {
                filter = filter.Substring(1);
            }

            // Absolute paths not permitted.
            if (Path.IsPathRooted(filter))
            {
                return NullChangeToken.Singleton;
            }

            return _filesWatcher.CreateFileChangeToken(filter);
        }
    }
}