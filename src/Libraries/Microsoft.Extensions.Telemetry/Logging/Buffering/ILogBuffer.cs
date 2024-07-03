// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Logging.Sampling;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Represent a log buffering interface.
/// </summary>
public interface ILogBuffer
{
    /// <summary>
    /// Buffer the incoming <paramref name="logRecord"/> to the <paramref name="bufferName"/>.
    /// </summary>
    /// <param name="bufferName">The buffer name to buffer to.</param>
    /// <param name="logRecord">The log record to buffer.</param>
    void Buffer(string bufferName, LogRecordPattern logRecord); // TODO: consider using the actual LogRecord instead of LogRecordPattern?

    /// <summary>
    /// Flush the buffer <paramref name="bufferName"/>.
    /// </summary>
    /// <param name="bufferName">The buffer name to flush.</param>
    void Flush(string bufferName);
}
