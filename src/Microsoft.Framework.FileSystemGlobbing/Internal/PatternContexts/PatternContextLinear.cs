// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Internal.PatternContexts
{
    public abstract class PatternContextLinear
        : PatternContext<PatternContextLinear.FrameData>
    {
        public PatternContextLinear(ILinearPattern pattern)
        {
            Pattern = pattern;
        }

        public override void PushDirectory(DirectoryInfoBase directory)
        {
            // copy the current frame
            var frame = Frame;

            if (IsStackEmpty() || Frame.IsNotApplicable)
            {
                // when the stack is being initialized
                // or no change is required.
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

            PushDataFrame(frame);
        }

        public struct FrameData
        {
            public bool IsNotApplicable;
            public int SegmentIndex;
        }

        protected ILinearPattern Pattern { get; }

        protected bool IsLastSegment()
        {
            return Frame.SegmentIndex == Pattern.Segments.Count - 1;
        }

        protected bool TestMatchingSegment(string value)
        {
            if (Frame.SegmentIndex >= Pattern.Segments.Count)
            {
                return false;
            }

            return Pattern.Segments[Frame.SegmentIndex].Match(value, StringComparison.Ordinal);
        }
    }
}
