// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Owin.FileSystems
{
    /// <summary>
    /// A file system abstraction
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Locate a file at the given path
        /// </summary>
        /// <param name="subpath">The path that identifies the file</param>
        /// <param name="fileInfo">The discovered file if any</param>
        /// <returns>True if a file was located at the given path</returns>
        bool TryGetFileInfo(string subpath, out IFileInfo fileInfo);

        /// <summary>
        /// Enumerate a directory at the given path, if any
        /// </summary>
        /// <param name="subpath">The path that identifies the directory</param>
        /// <param name="contents">The contents if any</param>
        /// <returns>True if a directory was located at the given path</returns>
        bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents);
    }
}
