// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

public class BufferingTool
{
    internal Dictionary<string, BufferType> Buffers { get; set; }

    public BufferingTool(IOptions<LogSamplingOptions> samplingOptions)
    {
        var matchers = samplingOptions.Value.Matchers;

        // TODO: create actual buffer from matchers instead of the pseudocode below:
        Buffers = new Dictionary<string, BufferType>();
    }

    public void Buffer(string bufferName)
    {
        Buffers[bufferName].Buffer();
    }

    public void Flush(string bufferName)
    {
        Buffers[bufferName].Flush();
    }
}
