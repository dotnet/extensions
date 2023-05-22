// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Strategy to decide how outgoing HTTP path is logged.
/// </summary>
public enum OutgoingPathLoggingMode
{
    /// <summary>
    /// HTTP path is formatted, for example in a form of /foo/bar/redactedUserId.
    /// </summary>
    Formatted,

    /// <summary>
    /// HTTP path is not formatted, route parameters logged in curly braces, for example in a form of /foo/bar/{userId}.
    /// </summary>
    Structured
}
