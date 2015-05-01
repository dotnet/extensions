// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;

namespace Microsoft.Framework.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextRaggedInclude : PatternContextRagged
    {
        public PatternContextRaggedInclude(IRaggedPattern pattern)
            : base(pattern)
        {
        }

        public override void Declare(Action<IPathSegment, bool> onDeclare)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException("Can't declare path segment before enters any directory.");
            }

            if (Frame.IsNotApplicable)
            {
                return;
            }

            if (IsStartingGroup() && Frame.SegmentIndex < Frame.SegmentGroup.Count)
            {
                onDeclare(Frame.SegmentGroup[Frame.SegmentIndex], false);
            }
            else
            {
                onDeclare(WildcardPathSegment.MatchAll, false);
            }
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

            if (IsStartingGroup() && !TestMatchingSegment(directory.Name))
            {
                // deterministic not-included
                return false;
            }

            return true;
        }
    }
}
