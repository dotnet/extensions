// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Project constants.
/// </summary>
public static class RequestCheckpointConstants
{
    /// <summary>
    /// The time elapsed before the response headers have been sent to the client.
    /// </summary>
    public const string ElapsedTillHeaders = "elthdr";

    /// <summary>
    /// The time elapsed before the response has finished being sent to the client.
    /// </summary>
    public const string ElapsedTillFinished = "eltltf";

    /// <summary>
    /// The time elapsed before hitting the <see cref="CapturePipelineExitMiddleware"/> middleware.
    /// </summary>
    public const string ElapsedTillPipelineExitMiddleware = "eltexm";

    /// <summary>
    /// The time elapsed before the response back to middleware pipeline.
    /// </summary>
    public const string ElapsedResponseProcessed = "eltrspproc";

    /// <summary>
    /// The time elapsed before hitting the first middleware.
    /// </summary>
    public const string ElapsedTillEntryMiddleware = "eltenm";

    /// <summary>
    /// List of checkpoints added by the middlewares.
    /// </summary>
    internal static readonly string[] RequestCheckpointNames = new[]
    {
        ElapsedTillHeaders,
        ElapsedTillFinished,
        ElapsedTillEntryMiddleware,
        ElapsedTillPipelineExitMiddleware,
        ElapsedResponseProcessed
    };
}
