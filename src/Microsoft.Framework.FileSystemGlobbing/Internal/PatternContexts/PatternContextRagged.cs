// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Internal.PatternContexts
{
    public abstract class PatternContextRagged : PatternContext<PatternContextRagged.FrameData>
    {
        public PatternContextRagged(IRaggedPattern pattern)
        {
            Pattern = pattern;
        }

        public sealed override void PushDirectory(DirectoryInfoBase directory)
        {
            // copy the current frame
            var frame = Frame;

            if (IsStackEmpty())
            {
                // initializing
                frame.SegmentGroupIndex = -1;
                frame.SegmentGroup = Pattern.StartsWith;
            }
            else if (Frame.IsNotApplicable)
            {
                // no change
            }
            else if (IsStartingGroup())
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
            else if (!IsStartingGroup() && !IsEndingGroup() && TestMatchingGroup(directory))
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

            PushDataFrame(frame);
        }

        public struct FrameData
        {
            public bool IsNotApplicable;

            public int SegmentGroupIndex;

            public IList<IPathSegment> SegmentGroup;

            public int BacktrackAvailable;

            public int SegmentIndex;
        }

        protected IRaggedPattern Pattern { get; }

        protected bool IsStartingGroup()
        {
            return Frame.SegmentGroupIndex == -1;
        }

        protected bool IsEndingGroup()
        {
            return Frame.SegmentGroupIndex == Pattern.Contains.Count;
        }

        protected bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Frame.SegmentGroup.Count)
            {
                return false;
            }
            return Frame.SegmentGroup[Frame.SegmentIndex].Match(value, StringComparison.Ordinal);
        }

        protected bool TestMatchingGroup(FileSystemInfoBase value)
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
                if (!segment.Match(scan.Name, StringComparison.Ordinal))
                {
                    return false;
                }
                scan = scan.ParentDirectory;
            }
            return true;
        }
    }
}
