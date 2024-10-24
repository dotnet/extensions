// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface providing access to the current logging buffer.
/// </summary>
public interface ILoggingBufferProvider
{
    /// <summary>
    /// Gets current logging buffer.
    /// </summary>
    public ILoggingBuffer CurrentBuffer { get; }
}
