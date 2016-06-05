// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class StackTraceHelper
    {
        public static IList<StackFrameInfo> GetFrames(Exception exception)
        {
            var frames = new List<StackFrameInfo>();

            if (exception == null)
            {
                return frames;
            }

#if NET451
            using (var portablePdbReader = new PortablePdbReader())
#endif
            {
                var needFileInfo = true;
                var stackTrace = new System.Diagnostics.StackTrace(exception, needFileInfo);
                var stackFrames = stackTrace.GetFrames();

                if (stackFrames == null)
                {
                    return frames;
                }

                foreach (var frame in stackFrames)
                {
                    var method = frame.GetMethod();

                    var stackFrame = new StackFrameInfo
                    {
                        StackFrame = frame,
                        FilePath = frame.GetFileName(),
                        LineNumber = frame.GetFileLineNumber(),
                        Method = method.Name,
                    };

#if NET451
                    if (string.IsNullOrEmpty(stackFrame.FilePath))
                    {
                        // .NET Framework and older versions of mono don't support portable PDBs
                        // so we read it manually to get file name and line information
                        portablePdbReader.PopulateStackFrame(stackFrame, method, frame.GetILOffset());
                    }
#endif

                    frames.Add(stackFrame);

                }

                return frames;
            }
        }
    }
}
