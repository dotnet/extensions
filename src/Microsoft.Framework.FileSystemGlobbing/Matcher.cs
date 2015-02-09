// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Microsoft.Framework.FileSystemGlobbing.Internal;
using Microsoft.Framework.FileSystemGlobbing.Internal.Patterns;

namespace Microsoft.Framework.FileSystemGlobbing
{
    public class Matcher
    {
        private IList<IPattern> _includePatterns = new List<IPattern>();
        private IList<IPattern> _excludePatterns = new List<IPattern>();

        public Matcher AddInclude(string pattern)
        {
            _includePatterns.Add(PatternBuilder.Build(pattern));
            return this;
        }

        public Matcher AddExclude(string pattern)
        {
            _excludePatterns.Add(PatternBuilder.Build(pattern));
            return this;
        }

        public PatternMatchingResult Execute(DirectoryInfoBase directoryInfo)
        {
            var context = new MatcherContext(_includePatterns, _excludePatterns, directoryInfo);
            return context.Execute();
        }
    }
}