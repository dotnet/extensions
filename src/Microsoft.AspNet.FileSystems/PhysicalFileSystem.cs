// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Owin.FileSystems
{
    /// <summary>
    /// Looks up files using the on-disk file system
    /// </summary>
    public class PhysicalFileSystem : IFileSystem
    {
        // These are restricted file names on Windows, regardless of extension.
        private static readonly Dictionary<string, string> RestrictedFileNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "con", string.Empty },
            { "prn", string.Empty },
            { "aux", string.Empty },
            { "nul", string.Empty },
            { "com1", string.Empty },
            { "com2", string.Empty },
            { "com3", string.Empty },
            { "com4", string.Empty },
            { "com5", string.Empty },
            { "com6", string.Empty },
            { "com7", string.Empty },
            { "com8", string.Empty },
            { "com9", string.Empty },
            { "lpt1", string.Empty },
            { "lpt2", string.Empty },
            { "lpt3", string.Empty },
            { "lpt4", string.Empty },
            { "lpt5", string.Empty },
            { "lpt6", string.Empty },
            { "lpt7", string.Empty },
            { "lpt8", string.Empty },
            { "lpt9", string.Empty },
            { "clock$", string.Empty },
        };

        /// <summary>
        /// Creates a new instance of a PhysicalFileSystem at the given root directory.
        /// </summary>
        /// <param name="root">The root directory</param>
        public PhysicalFileSystem(string root)
        {
            Root = GetFullRoot(root);
            if (!Directory.Exists(Root))
            {
                throw new DirectoryNotFoundException(Root);
            }
        }

        /// <summary>
        /// The root directory for this instance.
        /// </summary>
        public string Root { get; private set; }

        private static string GetFullRoot(string root)
        {
            var applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var fullRoot = Path.GetFullPath(Path.Combine(applicationBase, root));
            if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                // When we do matches in GetFullPath, we want to only match full directory names.
                fullRoot += Path.DirectorySeparatorChar; 
            }
            return fullRoot;
        }

        private string GetFullPath(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(Root, path));
            if (!fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return fullPath;
        }

        /// <summary>
        /// Locate a file at the given path by directly mapping path segments to physical directories.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="fileInfo">The discovered file, if any</param>
        /// <returns>True if a file was discovered at the given path</returns>
        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            try
            {
                if (subpath.StartsWith("/", StringComparison.Ordinal))
                {
                    subpath = subpath.Substring(1);
                }
                var fullPath = GetFullPath(subpath);
                if (fullPath != null)
                {
                    var info = new FileInfo(fullPath);
                    if (info.Exists && !IsRestricted(info))
                    {
                        fileInfo = new PhysicalFileInfo(info);
                        return true;
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            fileInfo = null;
            return false;
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <param name="contents">The discovered directories, if any</param>
        /// <returns>True if a directory was discovered at the given path</returns>
        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            try
            {
                if (subpath.StartsWith("/", StringComparison.Ordinal))
                {
                    subpath = subpath.Substring(1);
                }
                var fullPath = GetFullPath(subpath);
                if (fullPath != null)
                {
                    var directoryInfo = new DirectoryInfo(fullPath);
                    if (!directoryInfo.Exists)
                    {
                        contents = null;
                        return false;
                    }

                    FileSystemInfo[] physicalInfos = directoryInfo.GetFileSystemInfos();
                    var virtualInfos = new IFileInfo[physicalInfos.Length];
                    for (int index = 0; index != physicalInfos.Length; ++index)
                    {
                        var fileInfo = physicalInfos[index] as FileInfo;
                        if (fileInfo != null)
                        {
                            virtualInfos[index] = new PhysicalFileInfo(fileInfo);
                        }
                        else
                        {
                            virtualInfos[index] = new PhysicalDirectoryInfo((DirectoryInfo)physicalInfos[index]);
                        }
                    }
                    contents = virtualInfos;
                    return true;
                }
            }
            catch (ArgumentException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (IOException)
            {
            }
            contents = null;
            return false;
        }

        private bool IsRestricted(FileInfo fileInfo)
        {
            string fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            return RestrictedFileNames.ContainsKey(fileName);
        }

        private class PhysicalFileInfo : IFileInfo
        {
            private readonly FileInfo _info;

            public PhysicalFileInfo(FileInfo info)
            {
                _info = info;
            }

            public long Length
            {
                get { return _info.Length; }
            }

            public string PhysicalPath
            {
                get { return _info.FullName; }
            }

            public string Name
            {
                get { return _info.Name; }
            }

            public DateTime LastModified
            {
                get { return _info.LastWriteTime; }
            }

            public bool IsDirectory
            {
                get { return false; }
            }

            public Stream CreateReadStream()
            {
                // Note: Buffer size must be greater than zero, even if the file size is zero.
                return new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024 * 64,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
        }

        private class PhysicalDirectoryInfo : IFileInfo
        {
            private readonly DirectoryInfo _info;

            public PhysicalDirectoryInfo(DirectoryInfo info)
            {
                _info = info;
            }

            public long Length
            {
                get { return -1; }
            }

            public string PhysicalPath
            {
                get { return _info.FullName; }
            }

            public string Name
            {
                get { return _info.Name; }
            }

            public DateTime LastModified
            {
                get { return _info.LastWriteTime; }
            }

            public bool IsDirectory
            {
                get { return true; }
            }

            public Stream CreateReadStream()
            {
                return null;
            }
        }
    }
}
