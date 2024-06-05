// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Enumerates actions one of which can be executed on a matching log record.
/// </summary>
public enum ControlAction
{
    /// <summary>
    /// Filter log records globally.
    /// </summary>
    GlobalFilter,

    /// <summary>
    /// Buffer log records globally.
    /// </summary>
    GlobalBuffer,

    /// <summary>
    /// Filter log records withing an HTTP request flow.
    /// </summary>
    RequestFilter,

    /// <summary>
    /// Buffer log records for the duration of an HTTP requst flow.
    /// </summary>
    RequestBuffer
}
