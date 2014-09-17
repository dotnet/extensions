// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.FileSystems
{
    /// <summary>
    /// Represents a directory's content in the file system.
    /// </summary>
#if ASPNET50 || ASPNETCORE50
    [Framework.Runtime.AssemblyNeutral]
#endif
    public interface IDirectoryContents : IEnumerable<IFileInfo>
    {
        /// <summary>
        /// True if a directory was located at the given path.
        /// </summary>
        bool Exists { get; }
    }
}