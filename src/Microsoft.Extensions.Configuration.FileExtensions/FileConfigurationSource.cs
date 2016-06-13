// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a base class for file based <see cref="IConfigurationSource"/>.
    /// </summary>
    public abstract class FileConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Used to access the contents of the file.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <summary>
        /// The path to the file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Determines if loading the file is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Determines whether the source will be loaded if the underlying file changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <summary>
        /// Builds the <see cref="IConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="IConfigurationProvider"/></returns>
        public abstract IConfigurationProvider Build(IConfigurationBuilder builder);
    }
}