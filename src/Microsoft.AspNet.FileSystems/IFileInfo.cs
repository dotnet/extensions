// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.IO;

namespace Microsoft.AspNet.FileSystems
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
