// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface for a global buffer manager.
/// </summary>
internal interface IGlobalBufferManager : IBufferManager
{
    /// <summary>
    /// Flushes the buffer and emits all buffered logs.
    /// </summary>
    void Flush();
}
