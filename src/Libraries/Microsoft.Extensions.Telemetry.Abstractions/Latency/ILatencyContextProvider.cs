// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// A factory of latency contexts.
/// </summary>
public interface ILatencyContextProvider
{
    /// <summary>
    /// Creates a new <see cref="ILatencyContext"/>.
    /// </summary>
    /// <returns>A new latency context.</returns>
    ILatencyContext CreateContext();
}
