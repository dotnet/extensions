// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class PatternContextRaggedInclude : PatternContextRagged
    {
        public PatternContextRaggedInclude(MatcherContext matcherContext, Pattern pattern) : base(matcherContext, pattern)
        {
        }

        public override bool Test(FileInfoBase file)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return IsEndsWith && TestMatchingGroup(file);
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (Frame.IsNotApplicable)
            {
                return false;
            }

            if (IsStartsWith && !TestMatchingSegment(directory.Name))
            {
                // deterministic not-included
                return false;
            }

            return true;
        }
    }
}
