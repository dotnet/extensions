// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.FileSystems
{
    /// <summary>
    /// Represents a file in the given file system.
    /// </summary>
#if ASPNET50 || ASPNETCORE50
    [Framework.Runtime.AssemblyNeutral]
#endif
    public interface IFileInfo
    {
        /// <summary>
        /// True if resource exists in the underlying storage system.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// The length of the file in bytes, or -1 for a directory or non-existing files.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// The path to the file, including the file name. Return null if the file is not directly accessible.
        /// </summary>
        string PhysicalPath { get; }

        /// <summary>
        /// The name of the file or directory, not including any path.
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

        /// <summary>
        /// True if the file is readonly.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Store new contents for resource. Folders will be created if needed. 
        /// </summary>
        void WriteContent(byte[] content);

        /// <summary>
        /// Deletes the file.
        /// </summary>
        void Delete();

        /// <summary>
        /// Gets a trigger to monitor the file changes. 
        /// </summary>
        /// <returns></returns>
        IExpirationTrigger CreateFileChangeTrigger();
    }
}