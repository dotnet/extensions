// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class PatternContextLinearInclude : PatternContextLinear
    {
        public PatternContextLinearInclude(MatcherContext matcherContext, Pattern pattern) : base(matcherContext, pattern)
        {
        }

        public override bool Test(FileInfoBase file)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return IsLastSegment && TestMatchingSegment(file.Name);
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return !IsLastSegment && TestMatchingSegment(directory.Name);
        }
    }
}
