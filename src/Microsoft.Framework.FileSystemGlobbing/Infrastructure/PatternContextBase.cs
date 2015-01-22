// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public abstract class PatternContextBase
    {
        public PatternContextBase(MatcherContext matcherContext, Pattern pattern)
        {
            MatcherContext = matcherContext;
            Pattern = pattern;
        }

        public MatcherContext MatcherContext { get; }

        public Pattern Pattern { get; }

        public abstract bool Test(DirectoryInfoBase directory);

        public abstract bool Test(FileInfoBase file);

        public abstract void PushFrame(DirectoryInfoBase directory);

        public abstract void PopFrame();
    }
}
