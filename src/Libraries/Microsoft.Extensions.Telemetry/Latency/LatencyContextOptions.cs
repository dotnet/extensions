// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Options for LatencyContext.
/// </summary>
public class LatencyContextOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether exception is thrown when using unregistered names.
    /// </summary>
    /// <remarks>The ILatencyContext APIs throws when using unregistred names if true.
    /// Becomes no-op otherwise. Defaults to false.</remarks>
    public bool ThrowOnUnregisteredNames { get; set; }
}
