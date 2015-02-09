// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextRaggedExclude : PatternContextRagged
    {
        public PatternContextRaggedExclude(IRaggedPattern pattern)
            : base(pattern)
        {
        }

        public override bool Test(FileInfoBase file)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't test file before enters any directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return IsEndingGroup() && TestMatchingGroup(file);
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't test directory before enters any directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return false;
            }

            if (IsEndingGroup() && TestMatchingGroup(directory))
            {
                // directory excluded with file-like pattern
                return true;
            }

            if (Pattern.EndsWith.Count == 0 &&
                Frame.SegmentGroupIndex == Pattern.Contains.Count - 1 &&
                TestMatchingGroup(directory))
            {
                // directory excluded by matching up to final '/**'
                return true;
            }

            return false;
        }
    }
}
