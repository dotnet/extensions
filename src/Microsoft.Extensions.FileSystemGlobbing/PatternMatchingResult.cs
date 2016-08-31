// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    /// <summary>
    /// Represents a collection of <see cref="FilePatternMatch" />
    /// </summary>
    public class PatternMatchingResult
    {
        /// <summary>
        /// Initializes the result with a collection of <see cref="FilePatternMatch" />
        /// </summary>
        /// <param name="files">A collection of <see cref="FilePatternMatch" /></param>
        public PatternMatchingResult(IEnumerable<FilePatternMatch> files)
        {
            Files = files;
        }

        /// <summary>
        /// A collection of <see cref="FilePatternMatch" />
        /// </summary>
        public IEnumerable<FilePatternMatch> Files { get; set; }
    }
}