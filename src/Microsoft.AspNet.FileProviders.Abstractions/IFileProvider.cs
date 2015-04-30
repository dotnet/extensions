// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.FileProviders
{
    /// <summary>
    /// A read-only file provider abstraction.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Locate a file at the given path.
        /// </summary>
        /// <param name="subpath">Relative path that identifies the file.</param>
        /// <returns>The file information. Caller must check Exists property.</returns>
        IFileInfo GetFileInfo(string subpath);

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">Relative path that identifies the directory.</param>
        /// <returns>Returns the contents of the directory.</returns>
        IDirectoryContents GetDirectoryContents(string subpath);

        /// <summary>
        /// Creates a change trigger with the specified filter.
        /// </summary>
        /// <param name="filter">Filter string used to determine what files or folders to monitor. Example: **/*.cs, *.*, subFolder/**/*.cshtml.</param>
        /// <returns>An <see cref="IExpirationTrigger"/> that is triggered when a file matching <paramref name="filter"/> is added, modified or deleted.</returns>
        IExpirationTrigger Watch(string filter);
    }
}