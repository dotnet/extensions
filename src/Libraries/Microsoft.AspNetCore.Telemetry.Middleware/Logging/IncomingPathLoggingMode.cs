// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Strategy to decide how request path is logged.
/// </summary>
public enum IncomingPathLoggingMode
{
    /// <summary>
    /// Request path is logged formatted, its params are not logged.
    /// </summary>
    Formatted,

    /// <summary>
    /// Request path is logged in a structured way (as route), its params are logged.
    /// </summary>
    Structured
}
