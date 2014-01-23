// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Owin.FileSystems
{
    /// <summary>
    /// Represents a file in the given file system.
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        /// The length of the file in bytes, or -1 for a directory info
        /// </summary>
        long Length { get; }

        /// <summary>
        /// The path to the file, including the file name.  Return null if the file is not directly accessible.
        /// </summary>
        string PhysicalPath { get; }

        /// <summary>
        /// The name of the file
        /// </summary>
        string Name { get; }

        /// <summary>
        /// When the file was last modified
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// True for the case TryGetDirectoryContents has enumerated a sub-directory
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// Return file contents as readonly stream. Caller should dispose stream when complete.
        /// </summary>
        /// <returns>The file stream</returns>
        Stream CreateReadStream();
    }
}
