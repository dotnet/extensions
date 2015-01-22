// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public abstract class PatternContextLinear : PatternContextWithFrame<PatternContextLinear.FrameData>
    {
        public PatternContextLinear(MatcherContext matcherContext, Pattern pattern) : base(matcherContext, pattern)
        {
        }

        public bool IsLastSegment
        {
            get { return Frame.SegmentIndex == Pattern.Segments.Count - 1; }
        }

        public bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Pattern.Segments.Count)
            {
                return false;
            }
            return Pattern.Segments[Frame.SegmentIndex].TestMatchingSegment(value, StringComparison.Ordinal);
        }

        public override void PushFrame(DirectoryInfoBase directory)
        {
            var frame = Frame;
            if (FrameStack.Count == 0)
            {
                // initializing
            }
            else if (Frame.IsNotApplicable)
            {
                // no change
            }
            else if (!TestMatchingSegment(directory.Name))
            {
                // nothing down this path is affected by this pattern
                frame.IsNotApplicable = true;
            }
            else
            {
                // directory matches segment, advance position in pattern
                frame.SegmentIndex = frame.SegmentIndex + 1;
            }

            PushFrame(frame);
        }

        public struct FrameData
        {
            public bool IsNotApplicable;

            public int SegmentIndex;
        }
    }
}
