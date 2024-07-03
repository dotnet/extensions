// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Logging.Sampling;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// A wrapper for log buffers.
/// </summary>
public class LogBuffer : ILogBuffer
{
    internal Dictionary<string, BufferType> Buffers { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogBuffer"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the <see cref="LogBuffer"/> type.</param>
    public LogBuffer(IOptions<LogBufferingOptions> options)
    {
        // TODO: create actual buffer from options instead of the pseudocode below:
        Buffers = new Dictionary<string, BufferType>();
    }

    /// <inheritdoc/>
    public void Buffer(string bufferName, LogRecordPattern logRecord)
    {
        Buffers[bufferName].Buffer(logRecord);
    }

    /// <inheritdoc/>
    public void Flush(string bufferName)
    {
        Buffers[bufferName].Flush();
    }
}
