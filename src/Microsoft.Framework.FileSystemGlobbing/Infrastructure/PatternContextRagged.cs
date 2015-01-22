// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public abstract class PatternContextRagged : PatternContextWithFrame<PatternContextRagged.FrameData>
    {
        public PatternContextRagged(MatcherContext matcherContext, Pattern pattern) : base(matcherContext, pattern)
        {
        }

        public bool IsStartsWith
        {
            get { return Frame.SegmentGroupIndex == -1; }
        }

        public bool IsEndsWith
        {
            get { return Frame.SegmentGroupIndex == Pattern.Contains.Count; }
        }

        public bool IsContains
        {
            get { return !IsStartsWith && !IsEndsWith; }
        }

        public override void PushFrame(DirectoryInfoBase directory)
        {
            var frame = Frame;
            if (FrameStack.Count == 0)
            {
                // initializing
                frame.SegmentGroupIndex = -1;
                frame.SegmentGroup = Pattern.StartsWith;
            }
            else if (Frame.IsNotApplicable)
            {
                // no change
            }
            else if (IsStartsWith)
            {
                if (!TestMatchingSegment(directory.Name))
                {
                    // nothing down this path is affected by this pattern
                    frame.IsNotApplicable = true;
                }
                else
                {
                    // starting path incrementally satisfied
                    frame.SegmentIndex += 1;
                }
            }
            else if (IsContains && TestMatchingGroup(directory))
            {
                frame.SegmentIndex = Frame.SegmentGroup.Count;
                frame.BacktrackAvailable = 0;
            }
            else
            {
                // increase directory backtrack length
                frame.BacktrackAvailable += 1;
            }

            while (
                frame.SegmentIndex == frame.SegmentGroup.Count &&
                frame.SegmentGroupIndex != Pattern.Contains.Count)
            {
                frame.SegmentGroupIndex += 1;
                frame.SegmentIndex = 0;
                if (frame.SegmentGroupIndex < Pattern.Contains.Count)
                {
                    frame.SegmentGroup = Pattern.Contains[frame.SegmentGroupIndex];
                }
                else
                {
                    frame.SegmentGroup = Pattern.EndsWith;
                }
            }

            PushFrame(frame);
        }

        public bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Frame.SegmentGroup.Count)
            {
                return false;
            }
            return Frame.SegmentGroup[Frame.SegmentIndex].TestMatchingSegment(value, StringComparison.Ordinal);
        }

        public bool TestMatchingGroup(FileSystemInfoBase value)
        {
            var groupLength = Frame.SegmentGroup.Count;
            var backtrackLength = Frame.BacktrackAvailable + 1;
            if (backtrackLength < groupLength)
            {
                return false;
            }

            var scan = value;
            for (int index = 0; index != groupLength; ++index)
            {
                var segment = Frame.SegmentGroup[groupLength - index - 1];
                if (!segment.TestMatchingSegment(scan.Name, StringComparison.Ordinal))
                {
                    return false;
                }
                scan = scan.ParentDirectory;
            }
            return true;
        }

        public struct FrameData
        {
            public bool IsNotApplicable;

            public int SegmentGroupIndex;

            public IList<PatternSegment> SegmentGroup;

            public int BacktrackAvailable;

            public int SegmentIndex;
        }
    }
}
