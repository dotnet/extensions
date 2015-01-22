// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class Matcher
    {
        public IList<Pattern> IncludePatterns = new List<Pattern>();
        public IList<Pattern> ExcludePatterns = new List<Pattern>();

        public Matcher AddInclude(string pattern)
        {
            IncludePatterns.Add(new Pattern(pattern));
            return this;
        }

        public Matcher AddExclude(string pattern)
        {
            ExcludePatterns.Add(new Pattern(pattern));
            return this;
        }

        public PatternMatchingResult Execute(DirectoryInfoBase directoryInfo)
        {
            var context = new MatcherContext(this, directoryInfo);
            return context.Execute();
        }
    }
}
